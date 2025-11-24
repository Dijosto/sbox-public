namespace Sandbox;

/// <summary>
/// Simulates VerletRope components in parallel during PrePhysicsStep
/// </summary>
internal sealed class VerletRopeGameSystem : GameObjectSystem
{
	public VerletRopeGameSystem( Scene scene ) : base( scene )
	{
		// Listen to StartFixedUpdate to run before physics
		Listen( Stage.StartFixedUpdate, -100, UpdateRopes, "UpdateRopes" );
	}

	void UpdateRopes()
	{
		var ropes = Scene.GetAll<VerletRope>();
		if ( ropes.Count() == 0 ) return;

		var timeDelta = Time.Delta;
		Sandbox.Utility.Parallel.ForEach( ropes, rope => rope.Simulate( timeDelta ) );
	}
}
