namespace Sandbox.Engine;

internal partial class GlobalContext
{
	/// <summary>
	/// The current active context
	/// </summary>
	public static GlobalContext Current;

	/// <summary>
	/// The context used for the menu system
	/// </summary>
	public static GlobalContext Menu;


	/// <summary>
	/// The context used for the game. This is the default context.
	/// </summary>
	public static GlobalContext Game;

	/// <summary>
	/// The global context for the game, which holds references to various systems and libraries used throughout the game.
	/// </summary>
	static GlobalContext()
	{
		Game = new GlobalContext();
		Menu = new GlobalContext();

		Current = Game;
	}

	/// <summary>
	/// Throws an exception when called from client or server.
	/// </summary>
	public static void AssertMenu( [System.Runtime.CompilerServices.CallerMemberName] string memberName = "" )
	{
		if ( Current != Menu )
			throw new System.Exception( $"{memberName} should only be called in Menu scope!" );
	}
}
