using System.IO;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	public int Main( StringWriter output, int extraParam )
	{
		output.Write( "Hello World!" );
		return 1;
	}

	public int Main( StringWriter output )
	{
		return 0;
	}
}
