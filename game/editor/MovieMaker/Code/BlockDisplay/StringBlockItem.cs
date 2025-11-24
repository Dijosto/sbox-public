using Sandbox.MovieMaker;

namespace Editor.MovieMaker.BlockDisplays;

#nullable enable

public sealed class StringBlockItem : PropertyBlockItem<string?>
{
	protected override void OnPaint()
	{
		base.OnPaint();

		if ( Block is IPaintHintBlock hintBlock )
		{
			foreach ( var hintRange in hintBlock.GetPaintHints( Block.TimeRange ) )
			{
				PaintRange( hintRange );
			}
		}
		else
		{
			PaintRange( Block.TimeRange );
		}
	}

	private void PaintRange( MovieTimeRange range )
	{
		if ( Block.GetValue( range.Start ) is not { } value ) return;

		var origin = Parent.Session.TimeToPixels( Block.TimeRange.Start );
		var left = Parent.Session.TimeToPixels( range.Start ) - origin;
		var right = Parent.Session.TimeToPixels( range.End ) - origin;

		Paint.SetPen( Color.White.WithAlpha( 0.5f ), 12f );
		Paint.DrawText( new Rect( left, LocalRect.Top, right - left, LocalRect.Height ), value );
	}
}
