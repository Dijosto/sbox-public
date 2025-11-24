using System.IO;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	public int Main( StringWriter output )
	{
		output.Write( "Hello Blorld!" );
		return 0;
	}
}
