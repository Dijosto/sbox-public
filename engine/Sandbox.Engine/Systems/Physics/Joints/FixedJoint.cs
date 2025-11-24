namespace Sandbox.Physics;

/// <summary>
/// A generic "rope" type constraint.
/// </summary>
/// <remarks>
/// TODO: How is this different from <see cref="SpringJoint"/>? Should they be merged?
/// </remarks>
public partial class FixedJoint : PhysicsJoint
{
	internal FixedJoint( HandleCreationData _ ) { }

	/// <summary>
	/// How springy and tight the joint will be in its movement.
	/// </summary>
	public PhysicsSpring SpringLinear
	{
		get => native.GetLinearSpring();
		set => native.SetLinearSpring( value );
	}

	/// <summary>
	/// How springy and tight the joint will be in its rotation.
	/// </summary>
	public PhysicsSpring SpringAngular
	{
		get => native.GetAngularSpring();
		set => native.SetAngularSpring( value );
	}
}
