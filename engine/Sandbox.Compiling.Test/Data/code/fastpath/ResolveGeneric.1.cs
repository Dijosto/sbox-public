using System;
using System.IO;
using System.Linq;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	private static int?[] PointOffsets;

	public int Main( StringWriter output )
	{
		var temp = PointOffsets;

		return 0;
	}
}
