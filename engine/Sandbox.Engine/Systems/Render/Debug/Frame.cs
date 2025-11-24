namespace Sandbox;

internal static partial class DebugOverlay
{
	public partial class Frame
	{
		internal static void Draw( ref Vector2 pos )
		{
			var frame = FrameStats.Current;

			Row( ref pos, "Objects", frame.ObjectsRendered );
			Row( ref pos, "Triangles", frame.TrianglesRendered );
			Row( ref pos, "Draw Calls", frame.DrawCalls );
			Row( ref pos, "Material Changes", frame.MaterialChanges );
			Row( ref pos, "Display Lists", frame.DisplayLists );
			Row( ref pos, "Views", frame.SceneViewsRendered );
			Row( ref pos, "Resolves", frame.RenderTargetResolves );
			Row( ref pos, "Vis Culls", frame.ObjectsCulledByVis );
			Row( ref pos, "Screensize Culls", frame.ObjectsCulledByScreenSize );
			Row( ref pos, "Shadowed Lights", frame.ShadowedLightsInView );
			Row( ref pos, "Unshadowed Lights", frame.UnshadowedLightsInView );
			Row( ref pos, "Shadow Maps", frame.ShadowMaps );
		}

		static void Row( ref Vector2 pos, string label, double objectsRendered )
		{
			var rect = new Rect( pos, new Vector2( 512, 14 ) );

			var scope = new TextRendering.Scope( "", Color.White.WithAlpha( 0.8f ), 11, "Roboto Mono", 600 );
			scope.Outline = new TextRendering.Outline { Color = Color.Black, Enabled = true, Size = 2 };
			scope.Text = label;

			Hud.DrawText( scope, rect with { Width = 100 }, TextFlag.RightCenter );

			scope.TextColor = objectsRendered > 0 ? Color.White : Color.White.WithAlpha( 0.5f );
			scope.Text = objectsRendered.ToString( "N0" );
			Hud.DrawText( scope, rect with { Left = rect.Left + 110 }, TextFlag.LeftCenter );

			pos.y += rect.Height;
		}
	}
}
