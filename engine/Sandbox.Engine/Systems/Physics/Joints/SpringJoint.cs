namespace Sandbox.Physics;

/// <summary>
/// A rope-like constraint that is has springy/bouncy.
/// </summary>
public partial class SpringJoint : PhysicsJoint
{
	internal SpringJoint( HandleCreationData _ ) { }

	/// <summary>
	/// How springy and tight the joint will be
	/// </summary>
	public PhysicsSpring SpringLinear
	{
		get => native.GetLinearSpring();
		set => native.SetLinearSpring( value );
	}

	/// <summary>
	/// Maximum length it should be allowed to go
	/// </summary>
	public float MaxLength
	{
		get => native.GetMaxLength();
		set => native.SetMaxLength( value );
	}

	/// <summary>
	/// Minimum length it should be allowed to go. At which point it acts a bit like a rod.
	/// </summary>
	public float MinLength
	{
		get => native.GetMinLength();
		set => native.SetMinLength( value );
	}

	/// <summary>
	/// Maximum force it should be allowed to go. Set to zero to only allow stretching.
	/// </summary>
	public float MaxForce
	{
		get => native.GetMaxForce();
		set => native.SetMaxForce( value );
	}

	/// <summary>
	/// Minimum force it should be allowed to go.
	/// </summary>
	public float MinForce
	{
		get => native.GetMinForce();
		set => native.SetMinForce( value );
	}

	[Obsolete( "doesn't exist, not used" )]
	public float ReferenceMass
	{
		get => default;
		set { }
	}
}
