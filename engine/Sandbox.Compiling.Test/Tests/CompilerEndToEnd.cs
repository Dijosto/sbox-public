namespace TestCompiler;

public partial class CompilerTest
{
	/*
	/// <summary>
	/// Compile an extension that relies on a package gamemode
	/// </summary>
	[TestMethod]
	public async Task PackageDependantEndtoEnd()
	{
		TypeLibrary typeLibrary = new TypeLibrary();
		typeLibrary.DebugMode = true;
		var ac = new AccessControl( "game" );

		//
		// Download sandbox Game
		//

		// get package
		var info = await Api.Packages.Get.RunAsync( "facepunch.sandbox" );
		Assert.IsNotNull( info.Package.Revision.Manifest );

		// download the assembly
		var gameDllInfo = info.Package.Revision.Manifest.Files.Single( x => x.Path == ".assembly" );
		var gameDll = await Sandbox.Utility.Web.GrabFile( $"https://files.facepunch.com/sbox/asset{gameDllInfo.Url}", default );

		// whitelist
		var gamePassed = ac.VerifyAssembly( new MemoryStream( gameDll ), out var trustedDll );
		Assert.IsTrue( gamePassed, string.Join( ",", ac.Errors ) );

		// register it
		typeLibrary.AddAssembly( new AssemblyRegistration( "facepunch.sandbox", trustedDll ) );


		//
		// Build runtime extension addon that relies on sandbox game
		//

		var codePath = System.IO.Path.GetFullPath( "compiler/code/" );
		var group = new CompileGroup();
		group.AddAssembly( "facepunch.sandbox", gameDll );

		var compilerSettings = new CompilerSettings();
		compilerSettings.Clean();

		var extensionCompiler = group.CreateCompiler( "extension", codePath + "/extension", compilerSettings );
		extensionCompiler.GeneratedCode.AppendLine( "global using static Sandbox.Internal.GlobalGameNamespace;" );
		extensionCompiler.AddReference( "facepunch.sandbox" );
		extensionCompiler.AddReference( "Sandbox.Game" );

		await group.BuildAsync();

		Assert.IsTrue( group.BuildResult.Success, group.BuildResult.BuildDiagnosticsString() );
		Assert.IsNotNull( group.BuildResult );
		Assert.AreEqual( group.BuildResult.Output.Count(), 1 );

		// whitelist
		bool extensionPassed = ac.VerifyAssembly( new MemoryStream( group.BuildResult.Output.FirstOrDefault().Value ), out var extensionDll );
		Assert.IsTrue( extensionPassed, string.Join( ",", ac.Errors ) );

		// register it
		typeLibrary.AddAssembly( new AssemblyRegistration( "extension", extensionDll ) );

	}
	*/
}
