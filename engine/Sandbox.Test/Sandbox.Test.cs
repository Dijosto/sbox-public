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
#if LIVE_UNIT_TEST
		Sandbox.Application.InitLiveUnitTest<TestInit>();
#else
		Sandbox.Application.InitUnitTest<TestInit>();
#endif

	}

	[AssemblyCleanup]
	public static void AssemblyCleanup()
	{
		Sandbox.Application.ShutdownUnitTest();

	}
}
