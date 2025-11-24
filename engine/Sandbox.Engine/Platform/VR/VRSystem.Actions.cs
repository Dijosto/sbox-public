using Facepunch.XR;

namespace Sandbox.VR;

partial class VRNative
{
	private static readonly string[] BooleanActionStrings = new[]
	{
		"/actions/default/in/joystick_button",
		"/actions/default/in/button_a",
		"/actions/default/in/button_b"
	};

	public enum BooleanAction
	{
		JoystickPress,
		ButtonA,
		ButtonB
	};

	private static readonly string[] FloatActionStrings = new[]
	{
		"/actions/default/in/grip",
		"/actions/default/in/trigger",
	};

	public enum FloatAction
	{
		Grip,
		Trigger
	};

	private static readonly string[] Vector2ActionStrings = new[]
	{
		"/actions/default/in/joystick",
	};

	public enum Vector2Action
	{
		Joystick
	};

	private static readonly string[] PoseActionStrings = new[]
	{
		"/actions/default/in/hand_pose",
		"/actions/default/in/vibrate_right",
	};

	public enum PoseAction
	{
		HandPose
	};

	private static readonly string[] HapticActionStrings = new[]
	{
		"/actions/default/out/vibrate_left",
		"/actions/default/out/vibrate_right",
	};

	public enum HapticAction
	{
		LeftHandHaptics,
		RightHandHaptics
	};

	internal static TrackedDeviceRole GetTrackedDeviceRoleForInputSource( InputSource source )
	{
		return source switch
		{
			InputSource.LeftHand => TrackedDeviceRole.LeftHand,
			InputSource.RightHand => TrackedDeviceRole.RightHand,
			InputSource.Head => TrackedDeviceRole.Head,

			_ => TrackedDeviceRole.Unknown,
		};
	}
}
