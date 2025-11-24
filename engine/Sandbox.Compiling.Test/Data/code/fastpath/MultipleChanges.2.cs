using System.IO;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	public void Testing( StringWriter output )
	{
	}

	public int Main( StringWriter output )
	{
		return 1;
	}
}
