namespace Sandbox.Engine;

internal partial class GlobalContext
{
	/// <summary>
	/// Should rarely have to get called, game scope is implicit. Will need to be called if we're 
	/// in the menu scope, and have to call something in the game scope.
	/// </summary>
	public static IDisposable GameScope() => new GlobalContextScope( Game );

	/// <summary>
	/// Should only be called at a really high level, when doing menu stuff
	/// </summary>
	public static IDisposable MenuScope() => new GlobalContextScope( Menu );


	public struct GlobalContextScope : IDisposable
	{
		GlobalContext previous;
		public GlobalContextScope( GlobalContext context )
		{
			previous = Current;
			Current = context;
		}

		public void Dispose()
		{
			Current = previous;
		}
	}
}
