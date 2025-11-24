namespace Sandbox.Physics;

/// <summary>
/// A ballsocket constraint.
/// </summary>
public partial class BallSocketJoint : PhysicsJoint
{
	internal BallSocketJoint( HandleCreationData _ ) { }

	Vector2 _swingLimit;
	bool _swingLimitEnabled;

	Vector2 _twistLimit;
	bool _twistLimitEnabled;

	/// <summary>
	/// Constraint friction.
	/// </summary>
	public float Friction
	{
		set => native.SetFriction( value );
	}

	/// <summary>
	/// Maximum angle it should be allowed to swing to
	/// </summary>
	public Vector2 SwingLimit
	{
		get => _swingLimit;
		set
		{
			if ( _swingLimit == value ) return;
			_swingLimit = value;
			native.SetLimit( "swing", _swingLimit );
		}
	}

	public bool SwingLimitEnabled
	{
		get => _swingLimitEnabled;
		set
		{
			if ( _swingLimitEnabled == value ) return;
			_swingLimitEnabled = value;
			native.SetLimitEnabled( "swing", _swingLimitEnabled );
		}
	}

	public Vector2 TwistLimit
	{
		get => _twistLimit;
		set
		{
			if ( _twistLimit == value ) return;
			_twistLimit = value;
			native.SetLimit( "twist", _twistLimit );
		}
	}

	public bool TwistLimitEnabled
	{
		get => _twistLimitEnabled;
		set
		{
			if ( _twistLimitEnabled == value ) return;
			_twistLimitEnabled = value;
			native.SetLimitEnabled( "twist", _twistLimitEnabled );
		}
	}
}
