using System.IO;
using System.Threading;

namespace Sandbox;

internal static partial class PackageManager
{
	static Logger log = new Logger( "PackageManager" );

	/// <summary>
	/// The library used to load assemblies
	/// </summary>
	internal static AccessControl AccessControl { get; } = new AccessControl();

	public static BaseFileSystem MountedFileSystem { get; private set; } = new AggregateFileSystem();
	public static HashSet<ActivePackage> ActivePackages { get; private set; } = new HashSet<ActivePackage>();

	/// <summary>
	/// Called when a new package is installed
	/// </summary>
	public static event Action<ActivePackage, string> OnPackageInstalledToContext;

	internal static void ResetForUnitTest()
	{
		ActivePackages = new();
		MountedFileSystem = new AggregateFileSystem();
	}

	static async Task<Package> FetchPackageAsync( string ident, bool localPriority )
	{
		if ( localPriority && Package.TryParseIdent( ident, out var parts ) && !parts.local )
		{
			if ( await Package.Fetch( $"{parts.org}.{parts.package}#local", false ) is Package package )
			{
				return package;
			}
		}

		return await Package.Fetch( ident, false );
	}


	/// <summary>
	/// Install a package
	/// </summary>
	internal static async Task<ActivePackage> InstallAsync( PackageLoadOptions options )
	{
		if ( options.PackageIdent == "local.base" )
			options.PackageIdent = "local.base#local";

		//
		// If this package exists then mark it with our tag and move on
		//
		var existingPackage = Find( options.PackageIdent, options.AllowLocalPackages );
		if ( existingPackage != null )
		{
			existingPackage.AddContextTag( options.ContextTag );
			log.Info( $"Install Package (Already Mounted) {options.PackageIdent} [{options.ContextTag}]" );
			return existingPackage;
		}

		log.Trace( $"Install Package {options.PackageIdent} [{options.ContextTag}]" );
		var package = await FetchPackageAsync( options.PackageIdent, options.AllowLocalPackages );

		options.CancellationToken.ThrowIfCancellationRequested();

		if ( package == null )
		{
			throw new FileNotFoundException( $"Unable to find package '{options.PackageIdent}'" );
		}

		//
		// If this package has dependencies then download them first
		//
		await InstallDependencies( package, options );

		var ap = await ActivePackage.Create( package, options.CancellationToken, options );
		options.CancellationToken.ThrowIfCancellationRequested();

		if ( package.IsRemote )
		{
			//
			// Games should always have code archives. If they don't then they probably pre-date code archives, and need to be updated.
			//
			if ( package.TypeName == "game" && !ap.HasCodeArchives() )
			{
				throw new System.Exception( "This game has no code archive!" );
			}

			if ( ap.HasCodeArchives() )
			{
				if ( !await ap.CompileCodeArchive() )
				{
					//
					// If there was a compile error in a game, report it to our backend so we can keep tabs.
					//
					if ( package.TypeName == "game" )
					{
						throw new System.Exception( "There were errors when compiling this game!" );
					}

					Log.Warning( "There were errors when compiling this game!" );
				}
			}
		}

		ap.AddContextTag( options.ContextTag );
		return ap;
	}

	public static void UnmountTagged( string tag )
	{
		log.Trace( $"Removing tags '{tag}'" );

		foreach ( var item in ActivePackages )
		{
			item.RemoveContextTag( tag );
		}

		UnmountUntagged();
	}

	private static void UnmountUntagged()
	{
		foreach ( var item in ActivePackages.Where( x => x.Tags.Count() == 0 ).ToArray() )
		{
			log.Trace( $"Unmounting '{item.Package.FullIdent}' - no tags remaining" );

			item.Delete();
			ActivePackages.Remove( item );
		}
	}

	private static async Task InstallDependencies( Package package, PackageLoadOptions options )
	{
		HashSet<string> dependancies = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

		bool hasLocalBase = false;

		//
		// This is the right way to reference packages. We should move everything else
		// to use this.
		//
		foreach ( var i in package.EnumeratePackageReferences() )
		{
			dependancies.Add( i );

			// if we have a gamemode reference - then that contains the base library!
			if ( package.TypeName == "game" )
			{
				hasLocalBase = true;
			}
		}

		if ( package is LocalPackage packageLocal )
		{
			//
			// Hack Sadface: If this is a local game then include the base as a dependency
			//
			if ( !hasLocalBase && packageLocal.NeedsLocalBasePackage() )
			{
				dependancies.Add( "local.base#local" );
			}
		}

		//
		// Install them all
		//
		foreach ( var packageName in dependancies )
		{
			await InstallAsync( options with { PackageIdent = packageName } );
			options.CancellationToken.ThrowIfCancellationRequested();
		}

		options.CancellationToken.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// Install all of the projects as packages
	/// </summary>
	internal static async Task InstallProjects( Project[] projects, CancellationToken token = default )
	{
		foreach ( var project in projects )
		{
			try
			{
				// install this package
				await InstallAsync( new PackageLoadOptions() { PackageIdent = project.Package.FullIdent, ContextTag = "local", CancellationToken = token, AllowLocalPackages = true } );
			}
			catch ( Exception ex )
			{
				log.Warning( ex, $"Error installing local package {project.Package.FullIdent}: {ex.Message}" );
			}
		}

		var removedPackages = ActivePackages
			.Where( x => x.Package is LocalPackage )
			.Where( x => !projects.Any( y => y.Package == x.Package ) )
			.ToArray();

		// loop through each local package
		// remove any that aren't in our list
		foreach ( var package in removedPackages )
		{
			package.RemoveContextTag( "local" );
			log.Trace( $"Remove local package {package.Package.FullIdent}" );
		}

		// we might have packages that can be removed now
		if ( removedPackages.Length > 0 )
		{
			UnmountUntagged();
		}
	}

	/// <summary>
	/// Retrieve a package by ident.
	/// </summary>
	internal static ActivePackage Find( string packageIdent )
	{
		return ActivePackages.Where( x => x.Package.IsNamed( packageIdent ) ).First();
	}

	/// <summary>
	/// Retrieve a package by ident and minimum download mode.
	/// </summary>
	internal static ActivePackage Find( string packageIdent, bool allowLocalPackages, bool exactName = false )
	{
		// don't search for exact name if it starts with local
		// because it might be #local, or not
		if ( packageIdent.StartsWith( "local." ) )
			exactName = false;

		return ActivePackages.FirstOrDefault( x =>
			(exactName ? string.Equals( x.Package.FullIdent, packageIdent, StringComparison.OrdinalIgnoreCase ) : x.Package.IsNamed( packageIdent ))
			&& (allowLocalPackages || x.Package is not LocalPackage) );
	}

	/// <summary>
	/// Tick hot-reload for all active packages that support it.
	/// Should be called from the main tick loop when in editor mode.
	/// </summary>
	internal static void TickHotReload()
	{
		if ( !Application.IsEditor )
			return;

		foreach ( var package in ActivePackages )
		{
			package.TickHotReload();
		}
	}

	/// <summary>
	/// Extract a published game's source code and assets to the current project.
	/// This allows editing and hot-reloading of published games in the editor.
	/// </summary>
	internal static async Task<bool> ExtractPublishedGameToProject( string gameIdent, CancellationToken token = default )
	{
		if ( Project.Current is null )
		{
			Log.Error( "No project is currently open" );
			return false;
		}

		log.Info( $"Extracting published game '{gameIdent}' to project '{Project.Current.Config.FullIdent}'" );

		// Fetch the package info
		var package = await Package.Fetch( gameIdent, false );
		if ( package is null )
		{
			Log.Error( $"Could not find package '{gameIdent}'" );
			return false;
		}

		if ( package is LocalPackage )
		{
			Log.Warning( $"Package '{gameIdent}' is already a local package" );
			return false;
		}

		// Download the package
		var packageFs = await package.Download( token, new PackageLoadOptions { PackageIdent = gameIdent } );
		if ( packageFs is null )
		{
			Log.Error( $"Failed to download package '{gameIdent}'" );
			return false;
		}

		try
		{
			var codePath = Project.Current.GetCodePath();
			var assetsPath = Project.Current.GetAssetsPath();

			// Create directories if they don't exist
			Directory.CreateDirectory( codePath );
			Directory.CreateDirectory( assetsPath );

			// Extract source code from .cll archives
			var codeArchives = packageFs.FindFile( "/", "*.cll", true ).ToArray();
			if ( codeArchives.Length > 0 )
			{
				log.Info( $"Extracting source from {codeArchives.Length} code archive(s)" );

				foreach ( var archivePath in codeArchives )
				{
					var bytes = packageFs.ReadAllBytes( archivePath );
					if ( bytes is null || bytes.Length <= 1 )
						continue;

					var archive = new CodeArchive( bytes );

					// Extract .cs files from syntax trees
					foreach ( var tree in archive.SyntaxTrees )
					{
						var filePath = tree.FilePath;
						if ( string.IsNullOrEmpty( filePath ) )
							continue;

						// Use the local path from the archive
						var relativePath = filePath.TrimStart( '/', '\\' );

						// Skip generated files
						if ( relativePath.StartsWith( "__gen_" ) )
							continue;

						var fullPath = Path.Combine( codePath, relativePath );
						var dir = Path.GetDirectoryName( fullPath );
						if ( !string.IsNullOrEmpty( dir ) && !Directory.Exists( dir ) )
						{
							Directory.CreateDirectory( dir );
						}

						var sourceText = tree.GetText().ToString();
						File.WriteAllText( fullPath, sourceText, System.Text.Encoding.UTF8 );
						log.Trace( $"Extracted: {relativePath}" );
					}

					// Extract additional files (like .razor files)
					foreach ( var file in archive.AdditionalFiles )
					{
						if ( string.IsNullOrEmpty( file.LocalPath ) )
							continue;

						var relativePath = file.LocalPath.TrimStart( '/', '\\' );
						var fullPath = Path.Combine( codePath, relativePath );
						var dir = Path.GetDirectoryName( fullPath );
						if ( !string.IsNullOrEmpty( dir ) && !Directory.Exists( dir ) )
						{
							Directory.CreateDirectory( dir );
						}

						File.WriteAllText( fullPath, file.Text, System.Text.Encoding.UTF8 );
						log.Trace( $"Extracted: {relativePath}" );
					}
				}
			}

			// Copy assets from the package (excluding code archives and .bin folder)
			var allFiles = packageFs.FindFile( "/", "*", true ).ToArray();
			foreach ( var file in allFiles )
			{
				// Skip code archives, binaries, and hidden folders
				if ( file.EndsWith( ".cll", StringComparison.OrdinalIgnoreCase ) )
					continue;
				if ( file.StartsWith( ".bin", StringComparison.OrdinalIgnoreCase ) || file.Contains( "/.bin" ) )
					continue;
				if ( file.StartsWith( "." ) )
					continue;

				try
				{
					var fileBytes = packageFs.ReadAllBytes( file );
					if ( fileBytes is null )
						continue;

					var relativePath = file.TrimStart( '/', '\\' );
					var fullPath = Path.Combine( assetsPath, relativePath );
					var dir = Path.GetDirectoryName( fullPath );
					if ( !string.IsNullOrEmpty( dir ) && !Directory.Exists( dir ) )
					{
						Directory.CreateDirectory( dir );
					}

					File.WriteAllBytes( fullPath, fileBytes.ToArray() );
					log.Trace( $"Copied asset: {relativePath}" );
				}
				catch ( Exception e )
				{
					log.Warning( $"Failed to copy asset '{file}': {e.Message}" );
				}
			}

			log.Info( $"Extraction complete. Source: {codePath}, Assets: {assetsPath}" );

			// Trigger rebuild of project compilers to pick up new source
			Project.RebuildCompilers();

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( e, $"Failed to extract game: {e.Message}" );
			return false;
		}
		finally
		{
			packageFs?.Dispose();
		}
	}
}

