namespace Sandbox.Diagnostics;

partial class PerformanceStats
{
	public class VRStats
	{
		/// <summary>
		/// How many frames have we rendered so far?
		/// </summary>
		[Obsolete]
		public uint NumFrames;

		/// <summary>
		/// How many frames have we missed so far?
		/// </summary>
		[Obsolete]
		public uint NumDroppedFrames;

		/// <summary>
		/// Number of frames that were reprojected as a fraction
		/// </summary>
		[Obsolete]
		public float ReprojectionRatio;

		/// <summary>
		/// Total GPU time in milliseconds
		/// </summary>
		[Obsolete]
		public float TotalRenderGpu;

		/// <summary>
		/// Total time the compositor took on the GPU, in milliseconds
		/// </summary>
		[Obsolete]
		public float CompositorRenderGpu;

		/// <summary>
		/// Total time the compositor took on the CPU, in milliseconds
		/// </summary>
		[Obsolete]
		public float CompositorRenderCpu;

		/// <summary>
		/// SteamVR supersampling scale as a fraction
		/// </summary>
		[Obsolete]
		public float ResolutionScale;

		/// <summary>
		/// Effective render resolution (base resolution multiplied by <see cref="ResolutionScale"/>), per-eye
		/// </summary>
		public Vector2 Resolution;

		/// <summary>
		/// IPD in millimetres
		/// </summary>
		public float InterpupillaryDistance;

		/// <summary>
		/// Total left controller battery percentage (0 to 100)
		/// </summary>
		[Obsolete]
		public float LeftControllerBatteryPercentage;

		/// <summary>
		/// Total right controller battery percentage (0 to 100)
		/// </summary>
		[Obsolete]
		public float RightControllerBatteryPercentage;

		/// <summary>
		/// Total headset battery percentage (0 to 100)
		/// </summary>
		[Obsolete]
		public float HeadsetBatteryPercentage;
	}

	/// <summary>
	/// Stats retrieved from the SteamVR compositor
	/// </summary>
	public static VRStats VR { get; internal set; } = new();
}
