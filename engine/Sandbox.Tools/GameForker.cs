using System;
using Sandbox;
using Sandbox.DataModel;
using System.IO;

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
	/// <param name="projectDir">Directory path where the forked project will be created</param>
	/// <param name="title">Display title for the new project</param>
	/// <param name="ident">Unique identifier for the new project</param>
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
	/// Extracts code from the already-mounted source game's CLL archives into
	/// the project's Code/ directory. The source game should already be installed
	/// via PackageManager with the "tools" tag before calling this.
	/// </summary>
	/// <param name="project">The forked project</param>
	/// <param name="sourceFs">The source game's mounted filesystem (from PackageManager.ActivePackage)</param>
	/// <param name="gameIdent">The source game's full ident (e.g. "org.game")</param>
	internal static async Task ExtractCodeIfNeeded( Project project, BaseFileSystem sourceFs, string gameIdent )
	{
		var codeDir = Path.Combine( project.GetRootPath(), "Code" );

		// Skip if Code/ already has files (already extracted on a previous open)
		if ( Directory.Exists( codeDir ) && Directory.EnumerateFiles( codeDir, "*", SearchOption.AllDirectories ).Any() )
		{
			Log.Info( "Code directory already has files, skipping extraction" );
			return;
		}

		Log.Info( $"Extracting code from '{gameIdent}'" );
		await ExtractCodeArchives( sourceFs, gameIdent, project.GetRootPath() );
	}

	/// <summary>
	/// Copy asset files from the source game's package filesystem into the project's
	/// Assets/ directory. Skips code archives (.bin/), ProjectSettings, and localization.
	/// Only runs on first open (skips if Assets/ already has files).
	/// </summary>
	internal static async Task ExtractAssetsIfNeeded( Project project, BaseFileSystem sourceFs )
	{
		var assetsDir = project.GetAssetsPath();

		// Skip if Assets/ already has files (already extracted on a previous open)
		if ( Directory.Exists( assetsDir ) && Directory.EnumerateFiles( assetsDir, "*", SearchOption.AllDirectories ).Any() )
		{
			Log.Info( "Assets directory already has files, skipping extraction" );
			return;
		}

		Directory.CreateDirectory( assetsDir );

		var allFiles = sourceFs.FindFile( "/", "*", true ).ToArray();
		Log.Info( $"Found {allFiles.Length} total files in source package" );

		int totalFiles = 0;

		foreach ( var file in allFiles )
		{
			var normalized = file.Replace( '\\', '/' ).TrimStart( '/' );

			// Skip code archives and binaries
			if ( normalized.StartsWith( ".bin", StringComparison.OrdinalIgnoreCase ) )
				continue;

			// Skip project settings and localization (these are separate concerns)
			if ( normalized.StartsWith( "ProjectSettings", StringComparison.OrdinalIgnoreCase ) )
				continue;

			if ( normalized.StartsWith( "localization", StringComparison.OrdinalIgnoreCase ) )
				continue;

			// Skip .meta files — they contain dependency checksums from the source package
			// that won't match the freshly extracted files. The asset system will regenerate
			// them with correct checksums on first scan.
			if ( normalized.EndsWith( ".meta", StringComparison.OrdinalIgnoreCase ) )
				continue;

			var bytes = await sourceFs.ReadAllBytesAsync( file );
			if ( bytes is null )
				continue;

			var outputPath = Path.Combine( assetsDir, normalized );
			Directory.CreateDirectory( Path.GetDirectoryName( outputPath ) );
			await File.WriteAllBytesAsync( outputPath, bytes.ToArray() );
			totalFiles++;
		}

		Log.Info( $"Extracted {totalFiles} asset file(s) to Assets/" );
	}

	/// <summary>
	/// Find all .cll files in the package filesystem, deserialize each CodeArchive,
	/// and write the source files to the project's Code/ directory.
	/// Each CLL belongs to a specific package (identified by CompilerName).
	/// Only extracts the game's own CLL — library CLLs are skipped since they're
	/// available via PackageReferences.
	/// </summary>
	static async Task ExtractCodeArchives( BaseFileSystem fs, string gameIdent, string projectDir )
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

			Log.Info( $"Archive '{cllPath}': compiler={archive.CompilerName}, {archive.SyntaxTrees.Count} source(s), {archive.AdditionalFiles.Count} additional(s)" );

			// Only extract CLLs that belong to the game itself.
			// Library CLLs (e.g. "base") are available via PackageReferences and
			// should not be extracted as source to avoid duplicate definitions.
			if ( !gameIdent.EndsWith( archive.CompilerName, StringComparison.OrdinalIgnoreCase ) )
			{
				Log.Info( $"Skipping library archive '{archive.CompilerName}' (not matching game '{gameIdent}')" );
				continue;
			}

			// Extract C# source files from syntax trees.
			// The CLL LocalPath is already a project-relative path (e.g. "MyFile.cs",
			// "UI/MainMenu.cs"). We use the FileMap to resolve physical paths back
			// to these local paths when needed.
			foreach ( var syntaxTree in archive.SyntaxTrees )
			{
				var filePath = syntaxTree.FilePath;

				// Use FileMap to convert physical paths to project-local paths
				if ( archive.FileMap.TryGetValue( filePath, out var mappedPath ) )
					filePath = mappedPath;

				var fileName = Path.GetFileName( filePath );

				// Skip compiler-generated files (__gen_*, __compiler_extra, etc.)
				if ( fileName.StartsWith( "__" ) )
					continue;

				var sourceText = syntaxTree.GetText().ToString();
				var outputPath = Path.Combine( projectDir, "Code", filePath.Replace( '\\', '/' ).TrimStart( '/' ) );

				Directory.CreateDirectory( Path.GetDirectoryName( outputPath ) );
				await File.WriteAllTextAsync( outputPath, sourceText );
				totalFiles++;
			}

			// Extract additional files (Razor files, etc.)
			foreach ( var additional in archive.AdditionalFiles )
			{
				if ( string.IsNullOrWhiteSpace( additional.LocalPath ) )
					continue;

				var outputPath = Path.Combine( projectDir, "Code", additional.LocalPath.Replace( '\\', '/' ).TrimStart( '/' ) );

				Directory.CreateDirectory( Path.GetDirectoryName( outputPath ) );
				await File.WriteAllTextAsync( outputPath, additional.Text );
				totalFiles++;
			}
		}

		Log.Info( $"Extracted {totalFiles} source file(s)" );
	}
}
