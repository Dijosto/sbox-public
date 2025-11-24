using Facepunch.ActionGraphs;
using Sandbox.ActionGraphs;
using Sandbox.Engine;
using Sandbox.Internal;
using System;
using System.Collections.Generic;

namespace ActionGraphs;

[TestClass]
public class LiveGamePackage
{
	[TestInitialize]
	public void TestInitialize()
	{
		Project.Clear();
		PackageManager.ResetForUnitTest();
		AssetDownloadCache.Initialize( $"{Environment.CurrentDirectory}/.source2/package_manager_folder" );
	}

	[TestCleanup]
	public void TestCleanup()
	{
		Game.NodeLibrary = null;
		GlobalContext.Current.FileMount = null;

		Game.Shutdown();
	}

	/// <summary>
	/// Asserts that all the ActionGraphs referenced by a given scene in a downloaded
	/// package have no errors.
	/// </summary>
	[TestMethod]
	[DataRow( "fish.sauna", 76972L, "scenes/finland.scene", 140,
		"d174cab5-7a05-476c-a545-4db2fd685032", // Prefab references game object from other scene
		"e9ac7c29-ff9f-4c3c-8d9d-7228c4711248", // Inventory method changed parameter types
		"462927b9-1f01-4ba8-9f6b-2e1e6a5934e4"  // Inventory method changed parameter types
	)]
	public async Task AssertNoGraphErrorsInScene( string packageName, long? version, string scenePath, int graphCount, params string[] ignoreGuids )
	{
		var ignoreGuidSet = new HashSet<Guid>( ignoreGuids.Select( Guid.Parse ) );

		using ( var packageLoader = new Sandbox.PackageLoader( "Test", GetType().Assembly ) )
		{
			using var enroller = packageLoader.CreateEnroller( "test-enroller" );

			GlobalContext.Current.FileMount = PackageManager.MountedFileSystem;

			enroller.OnAssemblyAdded = ( a ) =>
			{
				Game.TypeLibrary.AddAssembly( a.Assembly, true );
				Game.NodeLibrary.AddAssembly( a.Assembly );
			};

			Assert.AreEqual( 0, PackageManager.MountedFileSystem.FileCount, "No package files mounted" );

			var downloadOptions = new PackageLoadOptions
			{
				PackageIdent = version is { } v ? $"{packageName}#{v}" : packageName,
				ContextTag = "client",
				AllowLocalPackages = false
			};

			var activePackage = await PackageManager.InstallAsync( downloadOptions );

			if ( version is not null )
			{
				Assert.AreEqual( version.Value, activePackage.Package.Revision.VersionId );
			}

			Assert.IsNotNull( activePackage );
			Assert.AreNotEqual( 0, PackageManager.MountedFileSystem.FileCount, "We have package files mounted" );

			// Load the assemblies into the context
			enroller.LoadPackage( packageName );

			Assert.AreNotEqual( 0, GlobalGameNamespace.TypeLibrary.Types.Count, "Library has classes" );

			JsonUpgrader.UpdateUpgraders( GlobalGameNamespace.TypeLibrary );

			ResourceLoader.LoadAllGameResource( PackageManager.MountedFileSystem );

			var sceneFile = ResourceLibrary.Get<SceneFile>( scenePath );

			Assert.IsNotNull( sceneFile, "Target scene exists" );

			ActionGraphDebugger.Enabled = true;

			var anyErrors = false;

			Game.ActiveScene = new Scene();
			Game.ActiveScene.LoadFromFile( sceneFile.ResourcePath );

			var graphs = ActionGraphDebugger.GetAllGraphs();

			Assert.AreEqual( graphCount, graphs.Count, "Scene has expected graph count" );

			foreach ( var graph in graphs )
			{
				Console.WriteLine( $"{graph.Guid}: {graph.Title} {(ignoreGuidSet.Contains( graph.Guid ) ? "(IGNORED)" : "")}" );

				foreach ( var message in graph.Messages )
				{
					Console.WriteLine( $"  {message}" );
				}

				if ( !ignoreGuidSet.Contains( graph.Guid ) )
				{

					anyErrors |= graph.HasErrors();
				}
			}

			ActionGraphDebugger.Enabled = false;

			PackageManager.UnmountTagged( "client" );

			Assert.IsFalse( anyErrors );
		}

		Assert.AreEqual( 0, PackageManager.MountedFileSystem.FileCount, "Unmounted everything" );
	}
}
