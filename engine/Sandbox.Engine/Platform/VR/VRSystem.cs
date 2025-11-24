using Facepunch.XR;
using Sandbox.Utility;

namespace Sandbox.VR;

internal static partial class VRSystem
{
	public enum States
	{
		/// <summary>
		/// The VR system is currently shut down.
		/// </summary>
		Shutdown = 0,

		/// <summary>
		/// The VR system is not updating or rendering, but is ready.
		/// </summary>
		Standby,

		/// <summary>
		/// The VR system has begun to initialise. An OpenXR instance has been created.
		/// </summary>
		PreInit,

		/// <summary>
		/// The VR system is active and updating.
		/// </summary>
		Active,
	}

	public static States State { get; set; }
	public static bool IsActive => State == States.Active;

	public static bool WantsInit
	{
		get
		{
			if ( Application.IsStandalone && Standalone.Manifest.IsVRProject )
				return true;

			if ( CommandLine.HasSwitch( "-vr" ) || CommandLine.HasSwitch( "-vrdebug" ) )
				return true;

			// If you have a VR headset plugged in and active, you probably want the game to launch in vr mode
			return HasHeadset;
		}
	}
	public static bool WantsDebug => CommandLine.HasSwitch( "-vrdebug" );

	public static bool HasHeadset { get; set; }

	/// <summary>
	/// Initialise the VR instance.
	/// </summary>
	public static void Init()
	{
		if ( State == States.Active )
			return;

		if ( State < States.PreInit )
			PreInit();

		if ( !HasHeadset )
			return;

		VRNative.CreateCompositor();
		VRNative.Reset();
		State = States.Active;
	}

	public static void PreInit()
	{
		if ( Application.IsHeadless || CommandLine.HasSwitch( "-novr" ) )
		{
			HasHeadset = false;
			return;
		}

		HasHeadset = Instance.HasHeadset();

		if ( !HasHeadset )
			return;

		if ( State == States.PreInit )
			return;

		VRNative.CreateInstance();
		VRNative.Reset();
		State = States.PreInit;
	}

	[ConCmd( "vr_info", ConVarFlags.Protected )]
	public static void VrInfoConCommand()
	{
		if ( !IsActive )
		{
			Log.Info( "VR not initialized." );
			return;
		}

		Log.Info( $"Session state: {VRNative.SessionState}" );
		Log.Info( $"HMD state: {(IsHMDInStandby() ? "Standby" : "Focused")}" );
		Log.Info( $"Refresh rate: {VRNative.RefreshRate}Hz" );
		Log.Info( $"Distance between eyes (IPD): {VRNative.IPDMillimetres}mm" );
		Log.Info( $"Render target size: {VRNative.EyeRenderTargetSize}" );
		Log.Info( $"System name: {VRNative.GetSystemName()}" );

		Log.Info( $"Tracked objects:" );

		int i = 0;
		foreach ( var trackedObject in Input.VR.TrackedObjects )
		{
			var device = trackedObject._trackedDevice;

			if ( device.IsActive )
			{
				Log.Info( $"Device {i} type: {device.DeviceType}" );
				Log.Info( $"Device {i} role: {device.DeviceRole}" );
				Log.Info( $"Device {i} index: {device.DeviceIndex}" );

				Log.Info( $"Device {i} position: {device.Transform.Position}" );
				Log.Info( $"Device {i} rotation: {device.Transform.Rotation.Angles()}" );

				Log.Info( $"Device {i} input source: \"{device.InputSource}\"" );
				Log.Info( $"Device {i} input source handle: {device.InputSourceHandle}" );

				Log.Info( $"------------------------------------------------------------------------------------------------------------------------" );
			}

			i++;
		}
	}

	internal static bool IsRendering = false;

	internal static void FrameStart()
	{
		if ( !IsActive )
			return;

		VRNative.Update();

		var leftHandDevice = new TrackedDevice( InputSource.LeftHand );
		var rightHandDevice = new TrackedDevice( InputSource.RightHand );

		VRInput.Current ??= new VRInput( leftHandDevice, rightHandDevice );
		VRInput.Current.Update();
	}

	internal static void FrameEnd()
	{
		if ( !IsActive )
			return;
	}

	/// <summary>
	/// Disable the VR instance.
	/// </summary>
	public static void Disable()
	{
		if ( !IsActive )
			return;

		State = States.Standby;
	}

	/// <summary>
	/// Shut down the VR instance.
	/// </summary>
	public static void Shutdown()
	{
		if ( !IsActive )
			return;

		State = States.Shutdown;
	}

	internal static void RenderOverlays()
	{

	}

	internal static void Reset()
	{
		VRNative.Reset();
	}


	public static bool IsHMDInStandby()
	{
		return VRNative.IsHMDInStandby();
	}

	internal static PerformanceStats.VRStats GetPerformanceStats()
	{
		return new()
		{
			InterpupillaryDistance = VRNative.IPDMillimetres,
			Resolution = VRNative.EyeRenderTargetSize,
		};
	}
}
