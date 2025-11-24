namespace Sandbox;

/// <summary>
/// Draws text in screenspace
/// </summary>
internal class TextSceneObject : SceneCustomObject
{
	public Vector2 ScreenPos { get; set; }
	public Vector2 ScreenSize { get; set; } = 1000f;

	/// <summary>
	/// this argument is short sighted and stupid, don't keep using it
	/// </summary>
	public float AngleDegrees { get; set; } = 0f;

	public TextFlag TextFlags { get; set; } = TextFlag.Center;

	public TextRendering.Scope TextBlock;

	public TextSceneObject( SceneWorld sceneWorld ) : base( sceneWorld )
	{
		RenderLayer = SceneRenderLayer.OverlayWithoutDepth;
		managedNative.ExecuteOnMainThread = false;
		TextBlock = TextRendering.Scope.Default;
	}

	public override void RenderSceneObject()
	{
		var pos = ScreenPos;

		if ( TextFlags.Contains( TextFlag.CenterHorizontally ) )
		{
			pos.x -= ScreenSize.x * 0.5f;
		}

		if ( TextFlags.Contains( TextFlag.CenterVertically ) )
		{
			pos.y -= ScreenSize.y * 0.5f;
		}

		if ( TextFlags.Contains( TextFlag.Bottom ) )
		{
			pos.y -= ScreenSize.y;
		}

		var rect = new Rect( pos, ScreenSize );

		if ( AngleDegrees == 0f )
		{
			Graphics.DrawText( rect, TextBlock, TextFlags );
		}
		else
		{
			Graphics.DrawText( rect, AngleDegrees, TextBlock, TextFlags );
		}
	}
}

internal class WorldTextSceneObject : SceneCustomObject
{
	public string Text { get; set; }
	public string FontName { get; set; } = "Roboto";
	public float FontSize { get; set; } = 12.0f;
	public float FontWeight { get; set; } = 500.0f;
	public TextFlag TextFlags { get; set; } = TextFlag.Center;
	public Color Color { get; set; } = Color.White;
	public bool IgnoreDepth { get; set; } = false;

	public WorldTextSceneObject( SceneWorld sceneWorld ) : base( sceneWorld )
	{
		RenderLayer = SceneRenderLayer.OverlayWithDepth;
		managedNative.ExecuteOnMainThread = false;
	}

	public override void RenderSceneObject()
	{
		Graphics.Attributes.SetCombo( "D_WORLDPANEL", 1 );
		Graphics.Attributes.SetCombo( "D_NO_ZTEST", IgnoreDepth ? 1 : 0 );

		// Set a dummy WorldMat matrix so that ScenePanelObject doesn't break the transforms.
		Matrix mat = Matrix.CreateRotation( Rotation.From( 0, 0, 0 ) );
		Graphics.Attributes.Set( "WorldMat", mat );

		Graphics.DrawText( new Rect( 0 ), Text, Color, FontName, FontSize, FontWeight, TextFlags | TextFlag.DontClip );
	}
}
