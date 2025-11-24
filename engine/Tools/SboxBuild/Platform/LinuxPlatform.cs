namespace Facepunch;

/// <summary>
/// Linux platform implementation
/// </summary>
internal class LinuxPlatform : Platform
{
	protected override string PlatformBaseName => "linuxsteamrt"; // Fucking make this just "linux" when port is more mature

	public override bool CompileSolution( string solutionName, bool forceRebuild = false )
	{
		return Utility.RunProcess( "make", $"-f {solutionName}.mak SHELL=/bin/bash", "src" );
	}
}
