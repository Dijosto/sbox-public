using NativeEngine;

namespace Sandbox.Rendering;

internal class TiledCullingLayer : ProceduralRenderLayer
{
	public TiledCullingLayer()
	{
		Name = "Tiled Culling";
		Flags |= LayerFlags.NeverRemove;
		Flags |= LayerFlags.DoesntModifyColorBuffers;
		Flags |= LayerFlags.DoesntModifyDepthStencilBuffer;
		Flags |= LayerFlags.NeedsPerViewLightingConstants;
	}

	internal override void OnRender()
	{
		NativeEngine.CSceneSystem.RenderTiledLightCulling( Graphics.Context, Graphics.SceneView, new RenderViewport( Graphics.Viewport ) );
	}
}
