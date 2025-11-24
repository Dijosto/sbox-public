using static Sandbox.Diagnostics.PerformanceStats;

namespace Sandbox.Diagnostics;

public static unsafe class Performance
{
	/// <summary>
	/// Record a frame state section in PerformanceStats
	/// </summary>
	public static ScopeSection Scope( string title )
	{
		if ( Application.IsUnitTest ) return default;
		return Timings.Get( title ).Scope();
	}

	/// <summary>
	/// This exists to allow the creation of performance scopes without
	/// </summary>
	public ref struct ScopeSection
	{
		internal Timings Source;
		internal FastTimer Timer;

		public void Dispose()
		{
			Source?.ScopeFinished( this );
		}
	}
}
