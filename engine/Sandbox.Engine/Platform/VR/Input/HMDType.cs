namespace Sandbox.VR;

internal enum HMDType
{
	Unknown,
	HTC,
	Valve,
	Oculus,
	Pico,
	HP,
	WindowsMixedReality,
	Bigscreen,
	Pimax,
}

partial class VRSystem
{
	private static readonly Dictionary<string, HMDType> HMDStringToTypeMap = new Dictionary<string, HMDType>
	{
		{ "unknown", HMDType.Unknown },
		{ "HTC", HMDType.HTC },
		{ "Valve", HMDType.Valve },
		{ "Oculus", HMDType.Oculus },
		{ "Meta", HMDType.Oculus },
		{ "Pico", HMDType.Pico },
		{ "HP", HMDType.HP },
		{ "WindowsMR", HMDType.WindowsMixedReality },
		{ "Bigscreen", HMDType.Bigscreen },
		{ "Pimax", HMDType.Pimax }
	};

	internal static HMDType GetHMDType()
	{
		var identifier = VRNative.GetSystemName();

		if ( identifier == null )
			return HMDType.Unknown;

		if ( HMDStringToTypeMap.TryGetValue( identifier, out var type ) )
			return type;

		return HMDType.Unknown;
	}
}
