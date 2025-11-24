using Facepunch.XR;

namespace Sandbox.VR;

/// <summary>
/// Represents a VR controller, along with its transform, velocity, and inputs.
/// </summary>
public sealed partial record VRController : TrackedObject
{
	internal Vector3 Position => Transform.Position;
	internal Rotation Rotation => Transform.Rotation;

	internal TrackedControllerType _type;

	internal VRController( TrackedDevice trackedDevice ) : base( trackedDevice ) { }

	private Transform _transform;
	public override Transform Transform => Input.VR.Anchor.ToWorld( _transform );

	/// <summary>
	/// Is this controller currently being represented using full hand tracking?
	/// </summary>
	public bool IsHandTracked { get; internal set; }

	internal override void Update()
	{
		base.Update();

		UpdateHaptics();

		Trigger = new AnalogInput( Trigger, VRNative.FloatAction.Trigger, _trackedDevice.InputSource );
		Grip = new AnalogInput( Grip, VRNative.FloatAction.Grip, _trackedDevice.InputSource );
		Joystick = new AnalogInput2D( Joystick, VRNative.Vector2Action.Joystick, _trackedDevice.InputSource );
		JoystickPress = new DigitalInput( JoystickPress, VRNative.BooleanAction.JoystickPress, _trackedDevice.InputSource );
		ButtonA = new DigitalInput( ButtonA, VRNative.BooleanAction.ButtonA, _trackedDevice.InputSource );
		ButtonB = new DigitalInput( ButtonB, VRNative.BooleanAction.ButtonB, _trackedDevice.InputSource );

		_handJoints.Pose = VRNative.GetHandPoseState( _trackedDevice.InputSource, MotionRange.Hand );
		_conformingJoints.Pose = VRNative.GetHandPoseState( _trackedDevice.InputSource, MotionRange.Controller );
		IsHandTracked = _handJoints.Pose.handPoseLevel == HandPoseLevel.FullyTracked;

		_transform = _trackedDevice.Transform;
	}

	/// <summary>
	/// Retrieves or creates a cached model that can be used to render this controller.
	/// </summary>
	public Model GetModel()
	{
		return Model.Cube;
	}
}
