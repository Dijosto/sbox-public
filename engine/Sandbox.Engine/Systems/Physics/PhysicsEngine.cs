
namespace Sandbox.Physics;

internal static class PhysicsEngine
{
	internal static void OnActive( PhysicsBody physicsBody, Transform transform, Vector3 velocity, Vector3 linearVelocity )
	{
		physicsBody.OnActive( transform, velocity, linearVelocity );
	}

	internal static void OnPhysicsJointBreak( PhysicsJoint joint )
	{
		if ( !joint.IsValid() ) return;
		joint.InternalJointBroken();
	}
}
