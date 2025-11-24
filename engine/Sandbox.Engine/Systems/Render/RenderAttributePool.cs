using System.Collections.Concurrent;

namespace Sandbox;

/// <summary>
/// A thread-safe pool for reusing <see cref="RenderAttributes"/> instances.
/// Returning to the pool clears the instance but does not free its memory, allowing for efficient reuse.
/// </summary>
internal class RenderAttributePool
{
	private readonly ConcurrentBag<RenderAttributes> Pool = [];

	/// <summary>
	/// Get a pooled <see cref="RenderAttributes"/> instance.
	/// Make sure you return it to the pool after use with <see cref="Return(RenderAttributes)"/>.
	/// </summary>
	public RenderAttributes Get()
	{
		if ( Pool.TryTake( out var ra ) )
			return ra;

		return new RenderAttributes();
	}

	/// <summary>
	/// Returns a <see cref="RenderAttributes"/> instance to the pool for reuse.
	/// Make sure you're done with it because it will be cleared.
	/// </summary>
	public void Return( RenderAttributes renderAttributes )
	{
		ArgumentNullException.ThrowIfNull( renderAttributes );

		renderAttributes.Clear( false ); // don't free our memory, we want to reuse it
		Pool.Add( renderAttributes );
	}
}
