using System.IO;

namespace TestPackage;

[Sandbox.Internal.SourceLocation( "def", 456 )]
public class Program : TestCompiler.IProgram
{
	public int Main( StringWriter output )
	{
		return 1;
	}
}
