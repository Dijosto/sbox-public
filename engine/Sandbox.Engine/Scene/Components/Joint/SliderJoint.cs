namespace Sandbox;

/// <summary>
/// Restrict an object to one axis, relative to another object. Like a drawer opening.
/// </summary>
[Expose]
[Title( "Slider Joint" )]
[Category( "Physics" )]
[Icon( "open_in_full" )]
[EditorHandle( "materials/gizmo/slider.png" )]
public sealed class SliderJoint : Joint
{
	/// <summary>
	/// Maximum length it should be allowed to go
	/// </summary>
	[Property]
	public float MaxLength
	{
		get;
		set
		{
			field = value;

			if ( _joint.IsValid() )
			{
				_joint.MaxLength = value;
				_joint.WakeBodies();
			}
		}
	}

	/// <summary>
	/// Minimum length it should be allowed to go
	/// </summary>
	[Property]
	public float MinLength
	{
		get;
		set
		{
			field = value;

			if ( _joint.IsValid() )
			{
				_joint.MinLength = value;
				_joint.WakeBodies();
			}
		}
	}

	/// <summary>
	/// Slider friction
	/// </summary>
	[Property]
	public float Friction
	{
		get;
		set
		{
			field = value;

			if ( _joint.IsValid() )
			{
				_joint.Friction = value;
				_joint.WakeBodies();
			}
		}
	}

	Physics.SliderJoint _joint;

	protected override PhysicsJoint CreateJoint( PhysicsPoint point1, PhysicsPoint point2 )
	{
		var localFrame1 = LocalFrame1;
		var localFrame2 = LocalFrame2;

		if ( Attachment == AttachmentMode.Auto )
		{
			localFrame1 = point1.LocalTransform;
			localFrame2 = point2.LocalTransform;
		}

		if ( !Scene.IsEditor )
		{
			LocalFrame1 = localFrame1;
			LocalFrame2 = localFrame2;

			Attachment = AttachmentMode.LocalFrames;
		}

		point1.LocalTransform = localFrame1;
		point2.LocalTransform = localFrame2;

		_joint = PhysicsJoint.CreateSlider( point1, point2, MinLength, MaxLength );
		_joint.Friction = Friction;

		_joint.WakeBodies();

		return _joint;
	}
}
