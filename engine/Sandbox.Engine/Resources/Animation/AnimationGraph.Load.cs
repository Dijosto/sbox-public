using NativeEngine;

namespace Sandbox;

public partial class AnimationGraph
{
	/// <summary>
	/// Load an animation graph from given file.
	/// </summary>
	public static AnimationGraph Load( string filename )
	{
		ThreadSafe.AssertIsMainThread();

		return FromNative( NativeGlue.Resources.GetAnimationGraph( filename ), filename );
	}

	/// <summary>
	/// Try to make it so only one AnimationGraph class exists for each animation graph
	/// </summary>
	internal static AnimationGraph FromNative( HAnimationGraph native, string name = null )
	{
		if ( native.IsNull || !native.IsStrongHandleValid() )
			return null;

		var instanceId = native.GetBindingPtr().ToInt64();
		if ( NativeResourceCache.TryGetValue<AnimationGraph>( instanceId, out var animgraph ) )
		{
			// If we're using a cached one we don't need this handle, we'll leak
			native.DestroyStrongHandle();
			return animgraph;
		}

		animgraph = new AnimationGraph( native, name ?? native.GetResourceName() );
		NativeResourceCache.Add( instanceId, animgraph );

		return animgraph;
	}
}
