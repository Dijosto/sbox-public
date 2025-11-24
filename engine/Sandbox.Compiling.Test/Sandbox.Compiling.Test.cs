global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using Sandbox;
global using System.Linq;
global using System.Threading.Tasks;
global using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

[TestClass]
public class TestInit
{
	[AssemblyInitialize]
	public static void ClassInitialize( TestContext context )
	{


	}

	[AssemblyCleanup]
	public static void AssemblyCleanup()
	{

	}
}
