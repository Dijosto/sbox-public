using System;
using System.IO;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	public Func<string> Example;

	public int Main( StringWriter output )
	{

		return 0;
	}
}
