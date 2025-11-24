using System.Threading.Channels;

namespace Sandbox;

[Expose]
public sealed class SceneAnimationSystem : GameObjectSystem<SceneAnimationSystem>
{
	private Channel<GameTransform> ChangedTransforms { get; set; } = Channel.CreateUnbounded<GameTransform>();

	public SceneAnimationSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.UpdateBones, 0, UpdateAnimation, "UpdateAnimation" );
		Listen( Stage.FinishUpdate, 0, FinishUpdate, "FinishUpdate" );
		Listen( Stage.PhysicsStep, 0, PhysicsStep, "PhysicsStep" );
	}

	void UpdateAnimation()
	{
		using ( PerformanceStats.Timings.Animation.Scope() )
		{
			var allSkinnedRenderers = Scene.GetAllComponents<SkinnedModelRenderer>().ToArray();

			// Skip out if we have a parent that is a skinned model, because we need to move relative to that
			// and their bones haven't been worked out yet. They will get worked out after our parent is.
			Parallel.ForEach(
				allSkinnedRenderers.Where( r =>
					r == r.GameObject.GetComponent<SkinnedModelRenderer>() &&
					r.Components.GetInAncestors<SkinnedModelRenderer>() is null
				),
				ProcessRenderer
			);

			while ( ChangedTransforms.Reader.TryRead( out var tx ) )
			{
				tx.TransformChanged( true );
			}

			//
			// Run events in the main thread
			//
			{
				foreach ( var x in allSkinnedRenderers )
				{
					if ( !x.IsValid ) continue;

					x.PostAnimationUpdate();
				}
			}
		}
	}

	void ProcessRenderer( SkinnedModelRenderer renderer )
	{
		renderer.Components.ExecuteEnabledInSelfAndDescendants<SkinnedModelRenderer>(
			s =>
			{
				if ( s.AnimationUpdate() )
				{
					ChangedTransforms.Writer.TryWrite( s.Transform );
				}
			} );
	}

	void FinishUpdate()
	{
		foreach ( var renderer in Scene.GetAllComponents<SkinnedModelRenderer>() )
		{
			if ( !renderer.IsValid() )
				continue;

			renderer.FinishUpdate();
		}
	}

	void PhysicsStep()
	{
		var renderers = Scene.GetAllComponents<SkinnedModelRenderer>()
			.Where( x => x.IsValid() )
			.ToArray();

		Parallel.ForEach( renderers, renderer => renderer.Physics?.Step() );
	}
}
