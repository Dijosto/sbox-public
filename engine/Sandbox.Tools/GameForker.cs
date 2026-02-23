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
	public static async Task<string> CreateProject( string sourceGameIdent, string projectDir, string title, string ident )
	{
		// Verify the source game exists
		var sourcePackage = await Package.Fetch( sourceGameIdent, false );
		if ( sourcePackage is null )
			throw new Exception( $"Could not find package '{sourceGameIdent}'" );

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

		// Store the source game ident so StartupLoadProject knows to extract code and mount assets
		config.SetMeta( "ForkedFrom", sourcePackage.FullIdent );

		var configPath = Path.Combine( projectDir, $"{ident}.sbproj" );
		File.WriteAllText( configPath, config.ToJson() );

		return configPath;
	}

	/// <summary>
	/// Called from StartupLoadProject on first editor open of a forked project.
	/// Downloads the source game package, extracts code from CLL archives into the
	/// project's Code/ directory, and installs assets.
	/// </summary>
	internal static async Task ExtractCodeIfNeeded( Project project, string forkedFrom, CancellationToken ct )
	{
		var codeDir = Path.Combine( project.GetRootPath(), "Code" );

		// Skip if Code/ already has files (already extracted)
		if ( Directory.Exists( codeDir ) && Directory.EnumerateFiles( codeDir, "*", SearchOption.AllDirectories ).Any() )
		{
			Log.Info( "Code directory already has files, skipping extraction" );
			return;
		}

		Log.Info( $"Extracting code from '{forkedFrom}'" );

		// Install the source package temporarily to access its filesystem
		var loadOptions = new PackageLoadOptions( forkedFrom, "fork", ct );
		var ap = await PackageManager.InstallAsync( loadOptions );

		try
		{
			await ExtractCodeArchives( ap.FileSystem, project.GetRootPath() );
		}
		finally
		{
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
