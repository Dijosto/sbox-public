namespace Sandbox;

internal static partial class DebugOverlay
{
	public partial class Allocations
	{
		static Sandbox.Diagnostics.Allocations.Scope _scope;
		static int _seconds = 0;
		static List<Sandbox.Diagnostics.Allocations.Entry> _entries = new( 32 );
		static RecordEntry _display;
		static RecordEntry _collect;

		struct RecordEntry
		{
			public long LowestPause;
			public long SumPause;
			public long HighestPause;
			public long Frames;
		}

		internal static void Disabled()
		{
			_scope?.Stop();
			_scope = null;
		}

		internal static void Draw( ref Vector2 pos )
		{
			_scope ??= new();
			_scope.Start();

			if ( _seconds != RealTime.Now.FloorToInt() )
			{
				_seconds = RealTime.Now.FloorToInt();

				_entries.Clear();
				_entries.AddRange( _scope.Entries.Take( 16 ).OrderByDescending( x => x.TotalBytes ) );

				_scope.Clear();

				_display = _collect;
				_collect = default;
				_collect.LowestPause = long.MaxValue;
			}

			_collect.Frames++;
			_collect.HighestPause = Math.Max( _collect.HighestPause, Sandbox.Diagnostics.PerformanceStats.GcPause );
			_collect.SumPause += Sandbox.Diagnostics.PerformanceStats.GcPause;
			_collect.LowestPause = Math.Min( _collect.LowestPause, Sandbox.Diagnostics.PerformanceStats.GcPause );

			if ( _display.Frames <= 0 )
				return;

			var x = pos.x;
			var y = pos.y;

			var ls = Sandbox.Diagnostics.PerformanceStats.LastSecond;

			var scope = new TextRendering.Scope( "", Color.White, 11, "Roboto Mono", 600 );
			scope.Outline = new TextRendering.Outline { Color = Color.Black, Enabled = true, Size = 2 };

			{
				var lowestPauseMs = TimeSpan.FromTicks( _display.LowestPause ).TotalMilliseconds;
				var highestPauseMs = TimeSpan.FromTicks( _display.HighestPause ).TotalMilliseconds;
				var pauseMs = TimeSpan.FromTicks( ls.GcPause ).TotalMilliseconds;
				var avgMs = TimeSpan.FromTicks( ls.GcPause / _display.Frames ).TotalMilliseconds;

				scope.Text = $"Gen: {ls.Gc0} / {ls.Gc1} / {ls.Gc2}\n" +
					$"Avg: {avgMs:N2}ms\n" +
					$"Min: {lowestPauseMs:N2}ms\n" +
					$"Max: {highestPauseMs:N2}ms\n" +
					$"Sum: {pauseMs:N2}ms ({(pauseMs / 1000.0) * 100.0:N2}%)";
				Hud.DrawText( scope, new Rect( x, y, 512, 13 ), TextFlag.LeftTop );

				y += 70;
			}

			foreach ( var e in _entries )
			{
				scope.TextColor = GetLineColor( e.Name );

				{
					scope.Text = e.TotalBytes.FormatBytes();
					Hud.DrawText( scope, new Rect( x, y, 50, 13 ), TextFlag.RightTop );
				}

				{
					scope.Text = e.Count.ToString( "N0" );
					Hud.DrawText( scope, new Rect( x, y, 75, 13 ), TextFlag.RightTop );
				}

				{
					scope.Text = e.Name;
					Hud.DrawText( scope, new Vector2( x + 80, y ), TextFlag.LeftTop );
				}

				y += 14;
			}

			pos.y = y;
		}

		static Color GetLineColor( string name )
		{
			if ( name.StartsWith( "System." ) ) return new Color( 0.7f, 1f, 0.7f );
			if ( name.StartsWith( "<GetAll>" ) ) return new Color( 1f, 1f, 0.7f );

			return Color.White;
		}
	}
}
