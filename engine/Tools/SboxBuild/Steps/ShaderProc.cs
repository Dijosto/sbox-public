using static Facepunch.Constants;

namespace Facepunch.Steps;

internal class ShaderProc( string name ) : Step( name )
{
	protected override ExitCode RunInternal()
	{
		Facepunch.ShaderProc.Program.Process( "engine" );
		return ExitCode.Success;
	}
}
