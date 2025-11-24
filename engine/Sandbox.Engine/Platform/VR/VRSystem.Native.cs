using NativeEngine;

namespace Sandbox.VR;

//
// Functions called from native
//
partial class VRSystem
{
	internal static bool InternalIsActive()
	{
		return IsActive;
	}

	internal static bool InternalWantsInit()
	{
		return WantsInit;
	}

	internal static void BeginFrame()
	{
		VRNative.BeginFrame();
	}

	internal static void EndFrame()
	{
		VRNative.EndFrame();
	}

	internal static bool Submit( IntPtr pColorTexture, IntPtr pDepthTexture )
	{
		var colorTexture = new ITexture( pColorTexture );
		var depthTexture = new ITexture( pDepthTexture );

		return VRNative.Submit( colorTexture, depthTexture );
	}

	internal static string GetVulkanInstanceExtensionsRequired()
	{
		var list = VRNative.GetVulkanInstanceExtensionsRequired();
		return string.Join( " ", list );
	}

	internal static string GetVulkanDeviceExtensionsRequired()
	{
		var list = VRNative.GetVulkanDeviceExtensionsRequired();
		return string.Join( " ", list );
	}
}
