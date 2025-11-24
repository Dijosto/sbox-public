using System.IO;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	public int Main( StringWriter output )
	{
		output.Write( typeof(Program).Assembly.FullName );

		return 0;
	}
}
