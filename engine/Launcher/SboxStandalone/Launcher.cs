namespace Sandbox;

public static class Launcher
{
	public static int Main()
	{
		var appSystem = new StandaloneAppSystem();
		appSystem.Run();

		return 0;
	}
}
