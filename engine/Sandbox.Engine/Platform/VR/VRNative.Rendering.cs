using Facepunch.XR;
using NativeEngine;
using System.Runtime.InteropServices;

namespace Sandbox.VR;

partial class VRNative
{
	[ConVar( "vr_enable_depth_submit", ConVarFlags.Protected, Help = "Enable submitting depth texture to compositor" )]
	public static bool EnableDepthSubmit { get; set; } = false;

	internal struct VRClipPlanes
	{
		public float ZNear;
		public float ZFar;

		public VRClipPlanes()
		{
			ZNear = 1.0f;
			ZFar = 1000.0f;
		}
	}

	internal static VRClipPlanes ClipPlanes;

	private static uint ReadUInt32( IntPtr ptr )
	{
		var signedValue = Marshal.ReadInt32( ptr );
		return BitConverter.ToUInt32( BitConverter.GetBytes( signedValue ) );
	}

	private static uint ReadUInt64( IntPtr ptr )
	{
		var signedValue = Marshal.ReadInt64( ptr );
		return BitConverter.ToUInt32( BitConverter.GetBytes( signedValue ) );
	}

	internal static TrackedDevicePose LeftEyeRenderPose;
	internal static TrackedDevicePose RightEyeRenderPose;

	private static TextureSubmitInfo GetTextureSubmitInfoVulkan( ITexture hColorTexture, ITexture hDepthTexture )
	{
		var pVkColorTexture = g_pRenderDevice.GetDeviceSpecificTexture( hColorTexture );
		var vkColorTexture = Marshal.PtrToStructure<VulkanDeviceSpecificTexture_t>( pVkColorTexture );
		var sampleCount = (uint)Graphics.RenderMultiSampleToNum( g_pRenderDevice.GetTextureMultisampleType( hColorTexture ) );

		var vulkanTextureData = new TextureSubmitInfo()
		{
			image = vkColorTexture.m_pImage,
			format = vkColorTexture.m_nFormat,

			depthImage = 0,
			depthFormat = 0,

			sampleCount = sampleCount,

			poseLeft = LeftEyeRenderPose,
			poseRight = RightEyeRenderPose
		};

		return vulkanTextureData;
	}

	internal static void BeginFrame()
	{
		if ( Compositor == IntPtr.Zero )
			return;

		if ( !VRSystem.IsRendering )
			return;

		FpxrCheck( Compositor.BeginFrame() );
	}

	internal static void EndFrame()
	{
		if ( Compositor == IntPtr.Zero )
			return;

		if ( !VRSystem.IsRendering )
			return;

		FpxrCheck( Compositor.EndFrame() );
	}

	internal static unsafe bool Submit( ITexture colorTexture, ITexture depthTexture )
	{
		var textureSubmitInfo = GetTextureSubmitInfoVulkan( colorTexture, depthTexture );
		FpxrCheck( Compositor.Submit( textureSubmitInfo ) );
		return true;
	}

	internal static string GetVulkanInstanceExtensionsRequired()
	{
		if ( VRSystem.State < VRSystem.States.PreInit )
			VRSystem.PreInit();

		if ( !VRSystem.HasHeadset )
			return "";

		return Instance.GetRequiredInstanceExtensions();
	}

	internal static string GetVulkanDeviceExtensionsRequired()
	{
		if ( VRSystem.State < VRSystem.States.PreInit )
			VRSystem.PreInit();

		if ( !VRSystem.HasHeadset )
			return "";

		return Instance.GetRequiredDeviceExtensions();
	}
}
