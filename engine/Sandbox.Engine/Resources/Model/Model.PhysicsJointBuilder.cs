namespace Sandbox;

/// <summary>
/// Provides ability to generate a physics joint for a <see cref="Model"/> at runtime.
/// </summary>
public abstract class PhysicsJointBuilder
{
	internal struct JointDesc
	{
		public PhysicsJointType Type;
		public int Body1, Body2;
		public ushort Flags;
		public bool EnableCollision, EnableLinearLimit, EnableLinearMotor;
		public Vector3 LinearTargetVelocity;
		public float MaxForce;
		public bool EnableSwingLimit, EnableTwistLimit, EnableAngularMotor;
		public Vector3 AngularTargetVelocity;
		public float MaxTorque, LinearFrequency, LinearDamping, AngularFrequency, AngularDamping;
		public float LinearStrength, AngularStrength;
		public Transform Frame1, Frame2;
		public Vector2 LinearLimit, SwingLimit, TwistLimit;
	}

	internal JointDesc Desc;

	/// <summary>
	/// The index of the first body connected by the joint.
	/// </summary>
	public int Body1 { get => Desc.Body1; set => Desc.Body1 = value; }

	/// <summary>
	/// The index of the second body connected by the joint.
	/// </summary>
	public int Body2 { get => Desc.Body2; set => Desc.Body2 = value; }

	/// <summary>
	/// The joint frame in the local space of <see cref="Body1"/>.
	/// </summary>
	public Transform Frame1 { get => Desc.Frame1; set => Desc.Frame1 = value; }

	/// <summary>
	/// The joint frame in the local space of <see cref="Body2"/>.
	/// </summary>
	public Transform Frame2 { get => Desc.Frame2; set => Desc.Frame2 = value; }

	/// <summary>
	/// Whether the connected bodies can collide with each other.
	/// </summary>
	public bool EnableCollision { get => Desc.EnableCollision; set => Desc.EnableCollision = value; }

	/// <summary>
	/// The maximum linear force the joint can withstand before breaking.
	/// </summary>
	public float LinearStrength { get => Desc.LinearStrength; set => Desc.LinearStrength = value; }

	/// <summary>
	/// The maximum angular force/torque the joint can withstand before breaking.
	/// </summary>
	public float AngularStrength { get => Desc.AngularStrength; set => Desc.AngularStrength = value; }

	protected PhysicsJointBuilder() { }
}

public static class PhysicsJointBuilderExtensions
{
	/// <inheritdoc cref="PhysicsJointBuilder.Body1"/>
	public static T WithBody1<T>( this T b, int v ) where T : PhysicsJointBuilder { b.Body1 = v; return b; }

	/// <inheritdoc cref="PhysicsJointBuilder.Body2"/>
	public static T WithBody2<T>( this T b, int v ) where T : PhysicsJointBuilder { b.Body2 = v; return b; }

	/// <inheritdoc cref="PhysicsJointBuilder.Frame1"/>
	public static T WithFrame1<T>( this T b, Transform v ) where T : PhysicsJointBuilder { b.Frame1 = v; return b; }

	/// <inheritdoc cref="PhysicsJointBuilder.Frame2"/>
	public static T WithFrame2<T>( this T b, Transform v ) where T : PhysicsJointBuilder { b.Frame2 = v; return b; }

	/// <inheritdoc cref="PhysicsJointBuilder.EnableCollision"/>
	public static T WithCollision<T>( this T b, bool v ) where T : PhysicsJointBuilder { b.EnableCollision = v; return b; }

	/// <inheritdoc cref="PhysicsJointBuilder.LinearStrength"/>
	public static T WithLinearStrength<T>( this T b, float v ) where T : PhysicsJointBuilder { b.LinearStrength = v; return b; }

	/// <inheritdoc cref="PhysicsJointBuilder.AngularStrength"/>
	public static T WithAngularStrength<T>( this T b, float v ) where T : PhysicsJointBuilder { b.AngularStrength = v; return b; }
}

/// <summary>
/// Provides ability to generate a hinge joint for a <see cref="Model"/> at runtime.
/// </summary>
public sealed class HingeJointBuilder : PhysicsJointBuilder
{
	/// <summary>
	/// Whether the hinge enforces a twist angle limit.
	/// </summary>
	public bool EnableTwistLimit { get => Desc.EnableTwistLimit; set => Desc.EnableTwistLimit = value; }

	/// <summary>
	/// The minimum and maximum allowed twist angles (degrees).
	/// </summary>
	public Vector2 TwistLimit { get => Desc.TwistLimit; set => Desc.TwistLimit = value; }

	/// <summary>
	/// Whether the hinge's angular motor is enabled.
	/// </summary>
	public bool EnableMotor { get => Desc.EnableAngularMotor; set => Desc.EnableAngularMotor = value; }

	/// <summary>
	/// Target angular velocity for the motor.
	/// </summary>
	public Vector3 TargetVelocity { get => Desc.AngularTargetVelocity; set => Desc.AngularTargetVelocity = value; }

	/// <summary>
	/// Maximum torque the motor may apply.
	/// </summary>
	public float MaxTorque { get => Desc.MaxTorque; set => Desc.MaxTorque = value; }

	/// <inheritdoc cref="TwistLimit"/>
	public HingeJointBuilder WithTwistLimit( float min, float max ) { TwistLimit = new Vector2( min, max ); EnableTwistLimit = true; return this; }

	/// <inheritdoc cref="TargetVelocity"/>
	public HingeJointBuilder WithTargetVelocity( Vector3 v ) { TargetVelocity = v; EnableMotor = true; return this; }

	/// <inheritdoc cref="MaxTorque"/>
	public HingeJointBuilder WithMaxTorque( float v ) { MaxTorque = v; return this; }

	internal HingeJointBuilder()
	{
		Desc.Type = PhysicsJointType.REVOLUTE_JOINT;
	}
}

/// <summary>
/// Provides ability to generate a ball joint for a <see cref="Model"/> at runtime.
/// </summary>
public sealed class BallJointBuilder : PhysicsJointBuilder
{
	/// <summary>
	/// Whether the joint enforces a swing angle limit.
	/// </summary>
	public bool EnableSwingLimit { get => Desc.EnableSwingLimit; set => Desc.EnableSwingLimit = value; }

	/// <summary>
	/// Whether the joint enforces a twist angle limit.
	/// </summary>
	public bool EnableTwistLimit { get => Desc.EnableTwistLimit; set => Desc.EnableTwistLimit = value; }

	/// <summary>
	/// Maximum allowed swing angle in degrees.
	/// </summary>
	public float SwingLimit { get => Desc.SwingLimit.y; set => Desc.SwingLimit = new Vector2( 0, value ); }

	/// <summary>
	/// Minimum and maximum allowed twist angles in degrees.
	/// </summary>
	public Vector2 TwistLimit { get => Desc.TwistLimit; set => Desc.TwistLimit = value; }

	/// <inheritdoc cref="SwingLimit"/>
	public BallJointBuilder WithSwingLimit( float v ) { SwingLimit = v; EnableSwingLimit = true; return this; }

	/// <inheritdoc cref="TwistLimit"/>
	public BallJointBuilder WithTwistLimit( float min, float max ) { TwistLimit = new Vector2( min, max ); EnableTwistLimit = true; return this; }

	internal BallJointBuilder()
	{
		Desc.Type = PhysicsJointType.SPHERICAL_JOINT;
	}
}

/// <summary>
/// Provides ability to generate a fixed joint for a <see cref="Model"/> at runtime.
/// </summary>
public sealed class FixedJointBuilder : PhysicsJointBuilder
{
	/// <summary>
	/// The frequency of the joint's linear spring in hertz.
	/// Higher values make the joint stiffer in translation.
	/// </summary>
	public float LinearFrequency { get => Desc.LinearFrequency; set => Desc.LinearFrequency = value; }

	/// <summary>
	/// The damping ratio for the joint's linear spring.
	/// Higher values reduce oscillation in translation.
	/// </summary>
	public float LinearDamping { get => Desc.LinearDamping; set => Desc.LinearDamping = value; }

	/// <summary>
	/// The frequency of the joint's angular spring in hertz.
	/// Higher values make the joint stiffer in rotation.
	/// </summary>
	public float AngularFrequency { get => Desc.AngularFrequency; set => Desc.AngularFrequency = value; }

	/// <summary>
	/// The damping ratio for the joint's angular spring.
	/// Higher values reduce oscillation in rotation.
	/// </summary>
	public float AngularDamping { get => Desc.AngularDamping; set => Desc.AngularDamping = value; }

	/// <inheritdoc cref="LinearFrequency"/>
	public FixedJointBuilder WithLinearFrequency( float v ) { LinearFrequency = v; return this; }

	/// <inheritdoc cref="LinearDamping"/>
	public FixedJointBuilder WithLinearDamping( float v ) { LinearDamping = v; return this; }

	/// <inheritdoc cref="AngularFrequency"/>
	public FixedJointBuilder WithAngularFrequency( float v ) { AngularFrequency = v; return this; }

	/// <inheritdoc cref="AngularDamping"/>
	public FixedJointBuilder WithAngularDamping( float v ) { AngularDamping = v; return this; }

	internal FixedJointBuilder()
	{
		Desc.Type = PhysicsJointType.WELD_JOINT;
	}
}

/// <summary>
/// Provides ability to generate a slider joint for a <see cref="Model"/> at runtime.
/// </summary>
public sealed class SliderJointBuilder : PhysicsJointBuilder
{
	/// <summary>
	/// Whether the joint enforces a translation limit along its axis.
	/// </summary>
	public bool EnableLimit { get => Desc.EnableLinearLimit; set => Desc.EnableLinearLimit = value; }

	/// <summary>
	/// The minimum and maximum allowed translation along the joint axis.
	/// </summary>
	public Vector2 Limit { get => Desc.LinearLimit; set => Desc.LinearLimit = value; }

	/// <inheritdoc cref="Limit"/>
	public SliderJointBuilder WithLimit( float min, float max ) { Limit = new Vector2( min, max ); EnableLimit = true; return this; }

	internal SliderJointBuilder()
	{
		Desc.Type = PhysicsJointType.PRISMATIC_JOINT;
	}
}

partial class ModelBuilder
{
	private readonly List<PhysicsJointBuilder> _joints = [];

	private static void InitJoint( PhysicsJointBuilder b, int body1, int body2, Transform? frame1, Transform? frame2, bool collision )
	{
		frame1 ??= Transform.Zero;
		frame2 ??= Transform.Zero;

		b.Body1 = body1;
		b.Body2 = body2;
		b.Frame1 = frame1.Value;
		b.Frame2 = frame2.Value;
		b.EnableCollision = collision;
	}

	/// <summary>
	/// Adds a hinge joint between two bodies, allowing rotation around a single axis.
	/// </summary>
	/// <param name="body1">The index of the first body.</param>
	/// <param name="body2">The index of the second body.</param>
	/// <param name="frame1">Optional joint frame in local space of body1.</param>
	/// <param name="frame2">Optional joint frame in local space of body2.</param>
	/// <param name="collision">Whether the connected bodies can collide.</param>
	public HingeJointBuilder AddHingeJoint( int body1, int body2, Transform? frame1 = default, Transform? frame2 = default, bool collision = false )
	{
		var b = new HingeJointBuilder();
		InitJoint( b, body1, body2, frame1, frame2, collision );
		_joints.Add( b );
		return b;
	}

	/// <summary>
	/// Adds a ball joint between two bodies, allowing free rotation within optional swing/twist limits.
	/// </summary>
	/// <param name="body1">The index of the first body.</param>
	/// <param name="body2">The index of the second body.</param>
	/// <param name="frame1">Optional joint frame in local space of body1.</param>
	/// <param name="frame2">Optional joint frame in local space of body2.</param>
	/// <param name="collision">Whether the connected bodies can collide.</param>
	public BallJointBuilder AddBallJoint( int body1, int body2, Transform? frame1 = default, Transform? frame2 = default, bool collision = false )
	{
		var b = new BallJointBuilder();
		InitJoint( b, body1, body2, frame1, frame2, collision );
		_joints.Add( b );
		return b;
	}

	/// <summary>
	/// Adds a fixed joint between two bodies, locking their relative position and orientation.
	/// </summary>
	/// <param name="body1">The index of the first body.</param>
	/// <param name="body2">The index of the second body.</param>
	/// <param name="frame1">Optional joint frame in local space of body1.</param>
	/// <param name="frame2">Optional joint frame in local space of body2.</param>
	/// <param name="collision">Whether the connected bodies can collide.</param>
	public FixedJointBuilder AddFixedJoint( int body1, int body2, Transform? frame1 = default, Transform? frame2 = default, bool collision = false )
	{
		var b = new FixedJointBuilder();
		InitJoint( b, body1, body2, frame1, frame2, collision );
		_joints.Add( b );
		return b;
	}

	/// <summary>
	/// Adds a slider joint between two bodies, allowing motion along a single axis.
	/// </summary>
	/// <param name="body1">The index of the first body.</param>
	/// <param name="body2">The index of the second body.</param>
	/// <param name="frame1">Optional joint frame in local space of body1.</param>
	/// <param name="frame2">Optional joint frame in local space of body2.</param>
	/// <param name="collision">Whether the connected bodies can collide.</param>
	public SliderJointBuilder AddSliderJoint( int body1, int body2, Transform? frame1 = default, Transform? frame2 = default, bool collision = false )
	{
		var b = new SliderJointBuilder();
		InitJoint( b, body1, body2, frame1, frame2, collision );
		_joints.Add( b );
		return b;
	}
}
