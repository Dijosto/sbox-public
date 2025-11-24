namespace Sandbox.Physics;

/// <summary>
/// The control joint is designed to control the movement of a body while remaining responsive to collisions.  
/// A spring can be used to control position and rotation, while a velocity motor can control velocity and  
/// simulate friction in top-down games. Both methods can be combined — for example, a spring with friction.  
/// Position and velocity control each have configurable force and torque limits.
/// </summary>
public partial class ControlJoint : PhysicsJoint
{
	internal ControlJoint( HandleCreationData _ ) { }

	/// <summary>
	/// The desired relative linear velocity.
	/// </summary>
	public Vector3 LinearVelocity
	{
		get => native.IsNull ? default : native.Motor_GetLinearVelocity();
		set
		{
			if ( native.IsNull ) return;
			native.Motor_SetLinearVelocity( value );
		}
	}

	/// <summary>
	/// The desired relative angular velocity in radians per second.
	/// </summary>
	public Vector3 AngularVelocity
	{
		get => native.IsNull ? default : native.Motor_GetAngularVelocity();
		set
		{
			if ( native.IsNull ) return;
			native.Motor_SetAngularVelocity( value );
		}
	}

	/// <summary>
	/// The joint maximum force.
	/// </summary>
	public float MaxVelocityForce
	{
		get => native.IsNull ? 0.0f : native.Motor_GetMaxVelocityForce();
		set
		{
			if ( native.IsNull ) return;
			native.Motor_SetMaxVelocityForce( value );
		}
	}

	/// <summary>
	/// The joint maximum torque.
	/// </summary>
	public float MaxVelocityTorque
	{
		get => native.IsNull ? 0.0f : native.Motor_GetMaxVelocityTorque();
		set
		{
			if ( native.IsNull ) return;
			native.Motor_SetMaxVelocityTorque( value );
		}
	}

	/// <summary>
	/// The spring linear hertz stiffness and damping ratio.
	/// </summary>
	public PhysicsSpring LinearSpring
	{
		get
		{
			if ( native.IsNull ) return default;
			return new PhysicsSpring
			{
				Frequency = native.Motor_GetLinearHertz(),
				Damping = native.Motor_GetLinearDampingRatio(),
				Maximum = native.Motor_GetMaxSpringForce()
			};
		}
		set
		{
			if ( native.IsNull ) return;
			native.Motor_SetLinearHertz( value.Frequency );
			native.Motor_SetLinearDampingRatio( value.Damping );
			native.Motor_SetMaxSpringForce( value.Maximum );
		}
	}

	/// <summary>
	/// The spring angular hertz stiffness and damping ratio.
	/// </summary>
	public PhysicsSpring AngularSpring
	{
		get
		{
			if ( native.IsNull ) return default;
			return new PhysicsSpring
			{
				Frequency = native.Motor_GetAngularHertz(),
				Damping = native.Motor_GetAngularDampingRatio(),
				Maximum = native.Motor_GetMaxSpringTorque()
			};
		}
		set
		{
			if ( native.IsNull ) return;
			native.Motor_SetAngularHertz( value.Frequency );
			native.Motor_SetAngularDampingRatio( value.Damping );
			native.Motor_SetMaxSpringTorque( value.Maximum );
		}
	}
}
