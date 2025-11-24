namespace Sandbox.Physics;

/// <summary>
/// A hinge-like constraint.
/// </summary>
public partial class HingeJoint : PhysicsJoint
{
	internal HingeJoint( HandleCreationData _ ) { }

	/// <summary>
	/// Maximum angle it should be allowed to go
	/// </summary>
	public float MaxAngle
	{
		get => native.GetMaxLength();
		set => native.SetMaxLength( value.DegreeToRadian() );
	}

	/// <summary>
	/// Minimum angle it should be allowed to go
	/// </summary>
	public float MinAngle
	{
		get => native.GetMinLength();
		set => native.SetMinLength( value.DegreeToRadian() );
	}

	public float Angle
	{
		get => native.GetAngle().RadianToDegree();
	}

	public Vector3 Axis
	{
		get
		{
			native.GetLocalFrameA( out _, out var rotation );
			return rotation * Vector3.Up;
		}
	}

	public float Speed => Axis.Dot( Body2.AngularVelocity - Body1.AngularVelocity );

	/// <summary>
	/// Hinge friction.
	/// </summary>
	public float Friction
	{
		set => native.SetFriction( value );
	}
}
