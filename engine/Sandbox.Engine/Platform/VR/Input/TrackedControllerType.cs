namespace Sandbox.VR;

internal enum TrackedControllerType
{
	Unknown,
	HTCVive,
	HTCViveFocus3,
	HTCViveCosmos,
	MetaTouch,
	ValveKnuckles,
	WindowsMixedReality,
	HPReverbG2,
	Generic
}

partial class VRSystem
{
	private static readonly Dictionary<string, TrackedControllerType> ControllerStringToTypeMap = new Dictionary<string, TrackedControllerType>
	{
		{ "unknown", TrackedControllerType.Unknown },
		{ "vive_controller", TrackedControllerType.HTCVive },
		{ "vive_focus3_controller", TrackedControllerType.HTCViveFocus3 },
		{ "vive_cosmos_controller", TrackedControllerType.HTCViveCosmos },
		{ "oculus_touch", TrackedControllerType.MetaTouch },
		{ "knuckles", TrackedControllerType.ValveKnuckles },
		{ "holographic_controller", TrackedControllerType.WindowsMixedReality },
		{ "hpmotioncontroller", TrackedControllerType.HPReverbG2 },
		{ "generic_tracked", TrackedControllerType.Generic },
	};

	internal static TrackedControllerType ControllerTypeFromString( string str )
	{
		if ( str == null )
			return TrackedControllerType.Unknown;

		if ( ControllerStringToTypeMap.TryGetValue( str, out var type ) )
			return type;

		return TrackedControllerType.Unknown;
	}
}
