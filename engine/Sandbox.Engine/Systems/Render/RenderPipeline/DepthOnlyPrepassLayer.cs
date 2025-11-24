namespace Sandbox.Rendering;

internal class DepthOnlyPrepassLayer : RenderLayer
{
	public DepthOnlyPrepassLayer( SceneViewRenderTargetHandle rtDepth )
	{
		Name = "Depth";
		LayerType = SceneLayerType.DepthPrepass;

		// Render all object shaders in Depth mode
		ShaderMode = "Depth";

		Flags |= LayerFlags.NeverRemove;
		Flags |= LayerFlags.IsDepthRenderingPass;

		DepthAttachment = rtDepth;

		ObjectFlagsRequired = SceneObjectFlags.IsOpaque;
		ObjectFlagsExcluded = SceneObjectFlags.IsLight | SceneObjectFlags.ExcludeGameLayer | SceneObjectFlags.NoZPrepass;

		// Discard pixels conservatively, our depth prepass shader doesn't do the fancy a2c smoothing math. ( But shadows do, so don't remove me! )
		Attributes.SetCombo( "D_ALPHA_TEST_CONSERVATIVE", 1 );
	}
}
