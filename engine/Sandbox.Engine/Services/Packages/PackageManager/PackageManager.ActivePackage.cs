using System.IO;
using System.Net;
using System.Threading;

namespace Sandbox;

internal static partial class PackageManager
{
	/// <summary>
	/// Describes a package that is currently mounted. Mounted packages are shared between client, server and editor.
	/// We keep track of which host is using which package using Tags.
	/// </summary>
	public class ActivePackage : ICompileReferenceProvider
	{
		public Package Package { get; private set; }
		public BaseFileSystem FileSystem { get; private set; }

		public PackageFileSystem PackageFileSystem { get; private set; }

		public BaseFileSystem AssemblyFileSystem { get; private set; }

		/// <summary>
		/// The project settings folder
		/// </summary>
		public BaseFileSystem ProjectSettings { get; private set; }

		/// <summary>
		/// The project's localization folder
		/// </summary>
		public BaseFileSystem Localization { get; private set; }

		public HashSet<string> Tags { get; } = new( StringComparer.OrdinalIgnoreCase );

		/// <summary>
		/// Mounted on FileSystem, this is where the codearchive is mounted to
		/// </summary>
		MemoryFileSystem memoryFileSystem;

		/// <summary>
		/// Whether the memory filesystem has been mounted to the main FileSystem
		/// </summary>
		bool memoryFileSystemMounted;

		/// <summary>
		/// Persistent compile group for hot-reload support in editor.
		/// Only created for published games when running in editor mode.
		/// </summary>
		CompileGroup compileGroup;

		/// <summary>
		/// The local filesystem path where extracted source is stored for editing.
		/// Only set when hot-reload is enabled (editor mode).
		/// </summary>
		public string ExtractedSourcePath => extractedSourcePath;
		string extractedSourcePath;

		/// <summary>
		/// Returns true if this package has hot-reload enabled.
		/// Hot-reload is only available for published games when running in editor mode.
		/// </summary>
		public bool HasHotReload => compileGroup != null;

		internal static async Task<ActivePackage> Create( Package package, CancellationToken token, PackageLoadOptions options )
		{
			var o = new ActivePackage();
			o.Package = package;

			if ( package is LocalPackage localPackage )
			{
				var projectSettingsPath = System.IO.Path.Combine( localPackage.Project.GetRootPath(), "ProjectSettings" );

				o.ProjectSettings = new AggregateFileSystem();
				if ( System.IO.Directory.Exists( projectSettingsPath ) )
				{
					o.ProjectSettings.CreateAndMount( projectSettingsPath );
				}

				o.Localization ??= new AggregateFileSystem();
				if ( System.IO.Directory.Exists( localPackage.LocalizationPath ) )
				{
					o.Localization.CreateAndMount( localPackage.LocalizationPath );
				}

				o.FileSystem = new AggregateFileSystem();

				if ( System.IO.Directory.Exists( localPackage.CodePath ) )
				{
					if ( localPackage.CodePath != null )
					{
						o.FileSystem.CreateAndMount( localPackage.CodePath );
					}
				}

				if ( System.IO.Directory.Exists( localPackage.ContentPath ) )
				{
					o.FileSystem.CreateAndMount( localPackage.ContentPath );
				}

				o.AssemblyFileSystem = new AggregateFileSystem();

				if ( Application.IsStandalone )
				{
					var binPath = Path.Combine( localPackage.Project.GetRootPath(), ".bin" );
					System.IO.Directory.CreateDirectory( binPath );
					o.AssemblyFileSystem.CreateAndMount( binPath );
				}
				else
				{
					o.AssemblyFileSystem.Mount( localPackage.AssemblyFileSystem );
				}

			}
			else
			{
				await o.DownloadAsync( token, options );
			}

			ActivePackages.Add( o );

			o.Mount();

			return o;
		}

		public void AddContextTag( string tag )
		{
			Tags.Add( tag );

			// this tag just became active
			OnPackageInstalledToContext?.Invoke( this, tag );
		}

		public void RemoveContextTag( string tag )
		{
			Tags.Remove( tag );
		}

		/// <summary>
		/// Set the filesystem up from this downloaded asset
		/// </summary>
		private async Task DownloadAsync( CancellationToken token, PackageLoadOptions options )
		{
			Assert.True( Package.IsRemote );

			PackageFileSystem = await Package.Download( token, options );

			if ( PackageFileSystem is null )
			{
				throw new WebException( $"Unable to download package '{Package.FullIdent}'" );
			}

			//
			// Mount downloaded filesystem as our main filesystem
			//
			FileSystem = new AggregateFileSystem();
			FileSystem.Mount( PackageFileSystem );

			//
			// Mount localization data from this package
			//
			Localization ??= new AggregateFileSystem();
			if ( FileSystem.DirectoryExists( "localization" ) )
			{
				// Mount as a subsystem of the package's FileSystem
				Localization.Mount( FileSystem.CreateSubSystem( "localization" ) );
			}

			//
			// If the ProjectSettings folder exists, we can create a filesystem for it.
			// If not, just create a memory filesystem, which will be empty, but at least won't be null.
			//
			if ( FileSystem.DirectoryExists( "ProjectSettings" ) )
			{
				ProjectSettings = FileSystem.CreateSubSystem( "ProjectSettings" );
			}
			else
			{
				ProjectSettings = new MemoryFileSystem();
			}

			//
			// Mount assembly from this package
			//
			AssemblyFileSystem ??= new AggregateFileSystem();
			if ( FileSystem.DirectoryExists( ".bin" ) )
			{
				// Mount as a subsystem of the package's FileSystem
				AssemblyFileSystem.Mount( FileSystem.CreateSubSystem( ".bin" ) );
			}
		}

		private void Mount()
		{
			MountedFileSystem.Mount( FileSystem );
			MountedFileSystem.Mount( AssemblyFileSystem );

			if ( Application.IsUnitTest ) // todo: fully init the engine for unit test
				return;

			// Reload any already resident resources with the ones we've just mounted
			NativeEngine.g_pResourceSystem.ReloadSymlinkedResidentResources();

			// Sandbox.FileSystem.Mounted.Mount( FileSystem );

			// this only makes sense if the package is a local package
			// Engine.SearchPath.Add( AbsolutePath, "GAME", true );
		}

		/// <summary>
		/// Called to unmount and remove this package from being active
		/// </summary>
		public void Delete()
		{
			// Clean up hot-reload compile group if it exists
			if ( compileGroup != null )
			{
				compileGroup.Dispose();
				compileGroup = null;
			}

			// Clean up extracted source directory
			if ( !string.IsNullOrEmpty( extractedSourcePath ) && Directory.Exists( extractedSourcePath ) )
			{
				try
				{
					Directory.Delete( extractedSourcePath, true );
				}
				catch ( System.Exception e )
				{
					Log.Warning( e, $"Failed to clean up extracted source: {e.Message}" );
				}
				extractedSourcePath = null;
			}

			MountedFileSystem.UnMount( FileSystem );
			MountedFileSystem.UnMount( AssemblyFileSystem );

			FileSystem.Dispose();
			FileSystem = default;

			PackageFileSystem?.Dispose();
			PackageFileSystem = null;

			AssemblyFileSystem.Dispose();
			AssemblyFileSystem = null;

			if ( Application.IsUnitTest ) // todo: fully init the engine for unit test
				return;

			// Reload any resident resources that were just unmounted (they shouldn't be used & will appear as an error, or a local variant)
			NativeEngine.g_pResourceSystem.ReloadSymlinkedResidentResources();
		}

		internal bool HasCodeArchives()
		{
			return FileSystem.FindFile( "/", "*.cll", true ).Any();
		}

		internal async Task<bool> CompileCodeArchive()
		{
			// get all the code archives
			var codeArchives = FileSystem.FindFile( "/", "*.cll", true ).ToArray();

			// It's okay for packages not to have code archives, but return as a fail
			if ( codeArchives.Count() == 0 )
				return false;

			var analytic = new Api.Events.EventRecord( "package.compile" );
			analytic.SetValue( "package", Package.FullIdent );
			analytic.SetValue( "version", Package.Revision?.VersionId );
			analytic.SetValue( "archives", codeArchives );

			Assert.AreNotEqual( 0, codeArchives.Length, "We have package files mounted" );

			// In editor mode, enable hot-reload by extracting source and keeping a persistent CompileGroup
			bool enableHotReload = Application.IsEditor;

			compileGroup = new CompileGroup( Package.Ident );
			compileGroup.AccessControl = AccessControl;
			compileGroup.ReferenceProvider = this;

			// If in editor, extract source for hot-reload support
			if ( enableHotReload )
			{
				extractedSourcePath = ExtractSourceFromArchives( codeArchives );
			}

			using ( analytic.ScopeTimer( "LoadArchives" ) )
			{
				foreach ( var file in codeArchives )
				{
					var bytes = await FileSystem.ReadAllBytesAsync( file );
					if ( bytes is null || bytes.Length <= 1 )
						throw new System.Exception( "Couldn't load code archive - error opening" );
					// Deserialize to a code archive
					var archive = new CodeArchive( bytes );
					// Create a compiler for it
					var compiler = compileGroup.GetOrCreateCompiler( archive.CompilerName );

					if ( enableHotReload && extractedSourcePath != null )
					{
						// Set up compiler to read from extracted source directory for hot-reload
						var compilerSourcePath = Path.Combine( extractedSourcePath, archive.CompilerName );
						if ( Directory.Exists( compilerSourcePath ) )
						{
							compiler.SetConfiguration( archive.Configuration );
							compiler.AddSourcePath( compilerSourcePath );
							foreach ( var reference in archive.References )
							{
								compiler.AddReference( reference );
							}
							compiler.MarkForRecompile();
							compiler.WatchForChanges();
						}
						else
						{
							// Fallback to archive-based compilation if source extraction failed
							compiler.UpdateFromArchive( archive );
						}
					}
					else
					{
						compiler.UpdateFromArchive( archive );
					}
				}
			}

			// Set up hot-reload callbacks if in editor
			if ( enableHotReload )
			{
				compileGroup.OnCompileSuccess = OnHotReloadCompileSuccess;
				compileGroup.PrintErrorsInConsole = true;
			}

			// Compile that bad boy
			using ( analytic.ScopeTimer( "Compile" ) )
			{
				await compileGroup.BuildAsync();
			}

			if ( !compileGroup.BuildResult.Success )
			{
				// Add an analytic so we can track these failures on the backend
				var er = new Api.Events.EventRecord( "package.compile.error" );
				er.SetValue( "package", Package.FullIdent );
				er.SetValue( "version", Package.Revision?.VersionId );
				er.SetValue( "errors", compileGroup.BuildResult.BuildDiagnosticsString( Microsoft.CodeAnalysis.DiagnosticSeverity.Error ) );
				er.Submit();

				// Clean up if not enabling hot-reload (no point keeping broken compile group)
				if ( !enableHotReload )
				{
					compileGroup.Dispose();
					compileGroup = null;
				}

				return false;
			}

			analytic.SetValue( "Diagnostics", compileGroup.BuildResult.Diagnostics
												.Where( x => x.Severity > Microsoft.CodeAnalysis.DiagnosticSeverity.Warning )
												.Select( x => new
												{
													x.Severity,
													x.Location?.SourceTree?.FilePath,
													x.Location?.GetLineSpan().StartLinePosition,
													Message = x.GetMessage()
												} )
												.ToArray() );

			// Should be successful
			Assert.True( compileGroup.BuildResult.Success );

			using ( analytic.ScopeTimer( "Write" ) )
			{
				WriteCompiledAssemblies();
			}

			analytic.Submit();

			// If not enabling hot-reload, dispose the compile group since we don't need it anymore
			if ( !enableHotReload )
			{
				compileGroup.Dispose();
				compileGroup = null;
			}
			else
			{
				Log.Info( $"Hot-reload enabled for {Package.FullIdent}. Source extracted to: {extractedSourcePath}" );
			}

			return true;
		}

		/// <summary>
		/// Extract source code from .cll archives to a local directory for editing.
		/// </summary>
		private string ExtractSourceFromArchives( string[] codeArchives )
		{
			try
			{
				// Create extraction directory in .source2/temp/package-source/{package-ident}
				var safeIdent = Package.Ident.Replace( ".", "_" ).Replace( "/", "_" );
				var basePath = EngineFileSystem.EditorTemporary?.GetFullPath( $"/package-source/{safeIdent}" );

				if ( string.IsNullOrEmpty( basePath ) )
				{
					Log.Warning( $"Could not get editor temporary path for source extraction" );
					return null;
				}

				// Clean up any existing extraction
				if ( Directory.Exists( basePath ) )
				{
					Directory.Delete( basePath, true );
				}
				Directory.CreateDirectory( basePath );

				foreach ( var archivePath in codeArchives )
				{
					var bytes = FileSystem.ReadAllBytes( archivePath );
					if ( bytes is null || bytes.Length <= 1 )
						continue;

					var archive = new CodeArchive( bytes );
					var compilerPath = Path.Combine( basePath, archive.CompilerName );
					Directory.CreateDirectory( compilerPath );

					// Extract .cs files from syntax trees
					foreach ( var tree in archive.SyntaxTrees )
					{
						var filePath = tree.FilePath;
						if ( string.IsNullOrEmpty( filePath ) )
							continue;

						// Convert to relative path if needed
						var relativePath = filePath;
						if ( Path.IsPathRooted( filePath ) )
						{
							relativePath = Path.GetFileName( filePath );
						}

						// Remove leading slashes
						relativePath = relativePath.TrimStart( '/', '\\' );

						var fullPath = Path.Combine( compilerPath, relativePath );
						var dir = Path.GetDirectoryName( fullPath );
						if ( !string.IsNullOrEmpty( dir ) && !Directory.Exists( dir ) )
						{
							Directory.CreateDirectory( dir );
						}

						var sourceText = tree.GetText().ToString();
						File.WriteAllText( fullPath, sourceText, System.Text.Encoding.UTF8 );
					}

					// Extract additional files (like .razor files)
					foreach ( var file in archive.AdditionalFiles )
					{
						if ( string.IsNullOrEmpty( file.LocalPath ) )
							continue;

						var relativePath = file.LocalPath.TrimStart( '/', '\\' );
						var fullPath = Path.Combine( compilerPath, relativePath );
						var dir = Path.GetDirectoryName( fullPath );
						if ( !string.IsNullOrEmpty( dir ) && !Directory.Exists( dir ) )
						{
							Directory.CreateDirectory( dir );
						}

						File.WriteAllText( fullPath, file.Text, System.Text.Encoding.UTF8 );
					}

					Log.Trace( $"Extracted source for {archive.CompilerName} to {compilerPath}" );
				}

				return basePath;
			}
			catch ( System.Exception e )
			{
				Log.Warning( e, $"Failed to extract source from archives: {e.Message}" );
				return null;
			}
		}

		/// <summary>
		/// Write compiled assemblies to the memory filesystem.
		/// </summary>
		private void WriteCompiledAssemblies()
		{
			memoryFileSystem ??= new MemoryFileSystem();
			memoryFileSystem.CreateDirectory( "/.bin" );

			foreach ( var assembly in compileGroup.BuildResult.Output )
			{
				Log.Trace( $"WRITE /.bin/{assembly.Compiler.AssemblyName}.dll" );
				memoryFileSystem.WriteAllBytes( $"/.bin/{assembly.Compiler.AssemblyName}.dll", assembly.AssemblyData );
			}

			// Mount if not already mounted
			if ( !memoryFileSystemMounted )
			{
				FileSystem.Mount( memoryFileSystem );
				memoryFileSystemMounted = true;
			}
		}

		/// <summary>
		/// Called when hot-reload compilation succeeds. Updates the assemblies.
		/// </summary>
		private void OnHotReloadCompileSuccess()
		{
			WriteCompiledAssemblies();
			Log.Info( $"Hot-reload: Recompiled {Package.FullIdent}" );
		}

		/// <summary>
		/// Check if the compile group needs recompilation and trigger it.
		/// Should be called from the main tick loop when in editor mode.
		/// </summary>
		internal void TickHotReload()
		{
			if ( compileGroup == null )
				return;

			if ( !compileGroup.NeedsBuild )
				return;

			if ( compileGroup.IsBuilding )
				return;

			compileGroup.AllowFastHotload = HotloadManager.hotload_fast;
			_ = compileGroup.BuildAsync();
		}

		public Microsoft.CodeAnalysis.PortableExecutableReference Lookup( string reference )
		{
			// we can't do anything unless it's in a package
			if ( !reference.StartsWith( "package." ) )
				return default;

			var targetAssemblyName = $"{reference}.dll";
			Log.Trace( $"ActivePackage: Looking for reference: {targetAssemblyName}" );

			//
			// Do any of the active packages have this dll?
			//
			foreach ( var package in ActivePackages )
			{
				if ( package == this )
					continue;

				// TODO - maybe we should filter to make sure the package has the same tag as us?

				var found = package.AssemblyFileSystem.FindFile( "/", targetAssemblyName, true ).FirstOrDefault();
				if ( found == null ) continue;

				var bytes = package.AssemblyFileSystem.ReadAllBytes( found ).ToArray();
				return Microsoft.CodeAnalysis.MetadataReference.CreateFromImage( bytes );
			}

			return default;
		}
	}
}
