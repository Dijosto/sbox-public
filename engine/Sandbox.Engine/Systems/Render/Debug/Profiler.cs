namespace Sandbox;

internal static partial class DebugOverlay
{
	public partial class Profiler
	{
		static Dictionary<string, float> _smoothedProfileWidth = new();

		internal static void Draw( ref Vector2 pos )
		{
			float labelWidth = 100;
			float height = 14;
			float y = pos.y;
			var left = pos.x;
			var mul = 200 / 16.0f;

			foreach ( var t in Sandbox.Diagnostics.PerformanceStats.Timings.GetMain() )
			{
				var rowRect = new Rect( left, y, 500, height );
				var text = t.Name;
				DebugOverlay.Hud.DrawText( new( text, t.Color.Lighten( 0.7f ), 11, "Roboto Mono", 600 ) { Outline = new TextRendering.Outline { Color = Color.Black, Size = 3, Enabled = true } }, rowRect with { Width = labelWidth }, TextFlag.RightCenter );

				rowRect = rowRect with { Left = left + labelWidth + 8 };

				var last = t.GetMetric( 1 );
				var avg = t.GetMetric( 256 );
				var aw = 4 + avg.Max * mul;

				if ( _smoothedProfileWidth.TryGetValue( t.Name, out var sw ) )
				{
					aw = MathX.LerpTo( sw, aw, Time.Delta * 10 );
				}

				_smoothedProfileWidth[t.Name] = aw;

				DebugOverlay.Hud.DrawRect( (rowRect with { Width = avg.Avg * mul }).Shrink( 1 ), t.Color.WithAlpha( 0.8f ), cornerRadius: new Vector4( 2 ) );
				DebugOverlay.Hud.DrawRect( (rowRect with { Width = last.Avg * mul }).Shrink( 1 ), t.Color.WithAlpha( 0.8f ), cornerRadius: new Vector4( 2 ) );
				DebugOverlay.Hud.DrawRect( rowRect with { Width = aw }, t.Color.WithAlpha( 0.2f ), borderWidth: new Vector4( 1 ), borderColor: Color.Black, cornerRadius: new Vector4( 2 ) );

				rowRect = rowRect with { Left = left + labelWidth + 16 + aw };

				text = $"{avg.Avg:N2}ms";

				if ( MathF.Abs( avg.Max - avg.Avg ) > 1 )
				{
					text += $" - {avg.Max:N2}ms";
				}

				DebugOverlay.Hud.DrawText( new( text, t.Color.Lighten( 0.7f ), 11, "Roboto Mono", 600 ) { Outline = new TextRendering.Outline { Color = Color.Black.WithAlpha( 0.8f ), Size = 2, Enabled = true } }, rowRect, TextFlag.LeftCenter );


				y += height + 2;
			}

			pos.y = y;
		}
	}
}
