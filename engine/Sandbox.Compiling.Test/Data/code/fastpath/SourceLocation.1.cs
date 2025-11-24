using System.IO;

namespace TestPackage;

[Sandbox.Internal.SourceLocation( "abc", 123 )]
public class Program : TestCompiler.IProgram
{
	public int Main( StringWriter output )
	{
		return 0;
	}
}
