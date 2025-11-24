using NativeEngine;

namespace Sandbox.Rendering;

internal class DepthNormalPrepassLayer : RenderLayer
{
	public DepthNormalPrepassLayer( bool large )
	{
		Name = $"Depth Normal Prepass {(large ? "Big" : "Small")}";
		LayerType = SceneLayerType.DepthPrepass;
		Flags |= LayerFlags.NeverRemove;
		ShaderMode = "Depth";

		if ( large )
		{
			Flags |= LayerFlags.DiscardColorBuffers;
			ClearFlags = ClearFlags.Color;

			// With fewer, larger objects we favor GPU perf over CPU perf by using fullsort
			Flags |= LayerFlags.NeedsFullSort;
		}

		ObjectFlagsRequired = SceneObjectFlags.IsOpaque;
		ObjectFlagsExcluded = SceneObjectFlags.IsLight | SceneObjectFlags.ExcludeGameLayer | SceneObjectFlags.NoZPrepass;

		// Discard pixels conservatively, our depth prepass shader doesn't do the fancy a2c smoothing math. ( But shadows do, so don't remove me! )
		Attributes.SetCombo( "D_ALPHA_TEST_CONSERVATIVE", 1 );
	}

	public void Setup( ISceneView view, RenderTarget gbufferColor, SceneViewRenderTargetHandle rtDepth )
	{
		ColorAttachment = gbufferColor.ToColorHandle( view );
		DepthAttachment = rtDepth;
	}
}
