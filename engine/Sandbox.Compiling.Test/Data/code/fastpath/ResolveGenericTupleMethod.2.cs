using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Sandbox;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	public static class ExampleClass<T>
	{
		public static void ExampleMethod( T value )
		{

		}
	}

	public int Main( StringWriter output )
	{
		ExampleClass<(int, int)[]>.ExampleMethod( default );

		return 1;
	}
}
