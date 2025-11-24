namespace Sandbox;

public partial class DebugOverlaySystem
{
	/// <summary>
	/// Draw a texture on the screen
	/// </summary>
	public void Texture( Texture texture, Vector2 position, Color? color = default, float duration = 0 )
	{
		var so = new QuadSceneObject( Scene.SceneWorld );
		so.ColorTint = color ?? Color.White;
		so.ScreenRect = new Rect( position, texture.Size );
		so.Flags.CastShadows = false;
		so.RenderLayer = SceneRenderLayer.OverlayWithoutDepth;
		so.Texture = texture;

		Add( duration, so );
	}

	/// <summary>
	/// Draw a texture on the screen
	/// </summary>
	public void Texture( Texture texture, Rect screenRect, Color? color = default, float duration = 0 )
	{
		var so = new QuadSceneObject( Scene.SceneWorld );
		so.ColorTint = color ?? Color.White;
		so.ScreenRect = screenRect;
		so.Flags.CastShadows = false;
		so.RenderLayer = SceneRenderLayer.OverlayWithoutDepth;
		so.Texture = texture;

		Add( duration, so );
	}
}
