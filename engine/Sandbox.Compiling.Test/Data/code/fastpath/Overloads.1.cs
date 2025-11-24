using System.IO;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	public int Main( StringWriter output, int extraParam )
	{
		return 1;
	}

	public int Main( StringWriter output )
	{
		return 0;
	}
}
