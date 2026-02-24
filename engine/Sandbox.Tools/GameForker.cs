using System;
using Sandbox;
using Sandbox.DataModel;
using System.IO;
using System.Threading;

namespace Editor;

/// <summary>
/// Handles forking a cloud game package into a local editable project.
/// Two-phase process:
/// 1. CreateProject (called from launcher) — creates .sbproj with ForkedFrom metadata
/// 2. ExtractCodeIfNeeded (called from StartupLoadProject) — extracts code from CLL archives on first editor open
/// </summary>
public static class GameForker
{
	static Logger Log = new( "GameForker" );

	/// <summary>
	/// Create a forked project directory and .sbproj file. This is a lightweight operation
	/// suitable for the launcher — it only creates files on disk, no package downloads.
	/// The heavy work (CLL extraction, asset loading) happens on first editor open via StartupLoadProject.
	/// </summary>
	/// <param name="sourceGame">The already-fetched source Package (avoids a redundant API call)</param>
	public static Task<string> CreateProject( Package sourceGame, string projectDir, string title, string ident )
	{
		// Create directory structure
		Directory.CreateDirectory( projectDir );
		Directory.CreateDirectory( Path.Combine( projectDir, "Code" ) );
		Directory.CreateDirectory( Path.Combine( projectDir, "Assets" ) );

		// Build the .sbproj config
		var config = new ProjectConfig();
		config.Title = title;
		config.Ident = ident;
		config.Org = "local";
		config.Type = "game";
		config.Schema = 1;

		// Copy PackageReferences from source game so library dependencies compile correctly
		if ( sourceGame.PackageReferences is { Length: > 0 } refs )
		{
			config.PackageReferences = new List<string>( refs );
		}

		// Store the source game ident so StartupLoadProject knows to extract code and mount assets
		config.SetMeta( "ForkedFrom", sourceGame.FullIdent );

		var configPath = Path.Combine( projectDir, $"{ident}.sbproj" );
		File.WriteAllText( configPath, config.ToJson() );

		return Task.FromResult( configPath );
	}

	/// <summary>
	/// Called from StartupLoadProject on first editor open of a forked project.
	/// Downloads the source game package, extracts code from CLL archives into the
	/// project's Code/ directory. Library dependencies are installed with the "tools"
	/// tag so they remain available for compilation after the source game is unmounted.
	/// </summary>
	internal static async Task ExtractCodeIfNeeded( Project project, string forkedFrom, CancellationToken ct )
	{
		var codeDir = Path.Combine( project.GetRootPath(), "Code" );

		// Skip if Code/ already has files (already extracted on a previous open)
		if ( Directory.Exists( codeDir ) && Directory.EnumerateFiles( codeDir, "*", SearchOption.AllDirectories ).Any() )
		{
			Log.Info( "Code directory already has files, skipping extraction" );
			return;
		}

		Log.Info( $"Extracting code from '{forkedFrom}'" );

		// Install the source game temporarily with a "fork" tag to get its filesystem.
		// SkipAssetDownload=true: we handle asset downloading separately via AssetSystem,
		// no need to pull all content files here too.
		var loadOptions = new PackageLoadOptions( forkedFrom, "fork", ct )
		{
			SkipAssetDownload = true
		};

		PackageManager.ActivePackage ap;
		try
		{
			ap = await PackageManager.InstallAsync( loadOptions );
		}
		catch ( Exception ex )
		{
			// If the source game fails to compile (e.g. has errors), log and continue.
			// The user can still try to edit the empty Code/ directory manually.
			Log.Warning( $"Could not install source game '{forkedFrom}': {ex.Message}" );
			Log.Warning( "Code extraction skipped — Code/ directory will be empty." );
			PackageManager.UnmountTagged( "fork" );
			return;
		}

		try
		{
			await ExtractCodeArchives( ap.FileSystem, project.GetRootPath() );

			// Install the source game's library dependencies with the "tools" tag so they
			// remain mounted for compilation after we unmount the source game below.
			// PackageManager.InstallAsync already tagged them "fork"; we just add "tools" to each.
			foreach ( var dep in ap.Package.EnumeratePackageReferences() )
			{
				await PackageManager.InstallAsync( new PackageLoadOptions( dep, "tools", ct )
				{
					SkipAssetDownload = true
				} );
			}
		}
		finally
		{
			// Unmount the source game (removes "fork" tag, unmounts if no other tags remain).
			// Library deps tagged "tools" stay mounted so our local Code/ can compile against them.
			// Built-in packages (tagged "local") also keep their tag and stay mounted.
			PackageManager.UnmountTagged( "fork" );
		}
	}

	/// <summary>
	/// Find all .cll files in the package filesystem, deserialize each CodeArchive,
	/// and write the source files to the project's Code/ directory.
	/// </summary>
	static async Task ExtractCodeArchives( BaseFileSystem fs, string projectDir )
	{
		var cllFiles = fs.FindFile( "/", "*.cll", true ).ToArray();
		if ( cllFiles.Length == 0 )
		{
			Log.Warning( "No code archives found in package" );
			return;
		}

		Log.Info( $"Found {cllFiles.Length} code archive(s)" );
		int totalFiles = 0;

		foreach ( var cllPath in cllFiles )
		{
			var bytes = await fs.ReadAllBytesAsync( cllPath );
			if ( bytes is null || bytes.Length <= 1 )
			{
				Log.Warning( $"Skipping empty archive: {cllPath}" );
				continue;
			}

			var archive = new CodeArchive( bytes );

			// Extract C# source files from syntax trees
			foreach ( var syntaxTree in archive.SyntaxTrees )
			{
				var filePath = syntaxTree.FilePath;

				// Use FileMap to convert physical paths to project-local paths
				if ( archive.FileMap.TryGetValue( filePath, out var mappedPath ) )
					filePath = mappedPath;

				// Skip generated files (they start with __gen_ or similar)
				if ( Path.GetFileName( filePath ).StartsWith( "__gen_" ) )
					continue;

				var sourceText = syntaxTree.GetText().ToString();
				var outputPath = Path.Combine( projectDir, "Code", NormalizePath( filePath ) );

				Directory.CreateDirectory( Path.GetDirectoryName( outputPath ) );
				await File.WriteAllTextAsync( outputPath, sourceText );
				totalFiles++;
			}

			// Extract additional files (Razor files, etc.)
			foreach ( var additional in archive.AdditionalFiles )
			{
				if ( string.IsNullOrWhiteSpace( additional.LocalPath ) )
					continue;

				var outputPath = Path.Combine( projectDir, "Code", NormalizePath( additional.LocalPath ) );

				Directory.CreateDirectory( Path.GetDirectoryName( outputPath ) );
				await File.WriteAllTextAsync( outputPath, additional.Text );
				totalFiles++;
			}
		}

		Log.Info( $"Extracted {totalFiles} source file(s)" );
	}

	/// <summary>
	/// Normalize a file path from the code archive to a safe relative path
	/// </summary>
	static string NormalizePath( string path )
	{
		// Remove any leading slashes or drive letters
		path = path.Replace( '\\', '/' );

		// Strip any absolute path prefix — keep only the relative part under Code/
		var codeIndex = path.IndexOf( "/Code/", StringComparison.OrdinalIgnoreCase );
		if ( codeIndex >= 0 )
			path = path.Substring( codeIndex + 6 ); // skip "/Code/"

		// Also handle "Code/" at the start
		if ( path.StartsWith( "Code/", StringComparison.OrdinalIgnoreCase ) )
			path = path.Substring( 5 );

		// Strip leading slashes
		path = path.TrimStart( '/' );

		return path;
	}
}
