using Sandbox.MovieMaker;
using Sandbox.Services;

namespace Editor.MovieMaker;

public class BackgroundItem : GraphicsItem
{
	public Session Session { get; }

	public BackgroundItem( Session session )
	{
		ZIndex = -10_000;
		Session = session;
	}

	protected override void OnPaint()
	{
		Paint.SetBrushAndPen( Timeline.BackgroundColor );
		Paint.DrawRect( LocalRect );

		if ( Session.SequenceTimeRange is { } sequenceRange )
		{
			Paint.SetBrushAndPen( Timeline.OuterColor );
			DrawTimeRangeRect( (MovieTime.Zero, Session.Duration) );

			Paint.SetBrushAndPen( Timeline.InnerColor );
			DrawTimeRangeRect( sequenceRange );
		}
		else
		{
			Paint.SetBrushAndPen( Timeline.InnerColor );
			DrawTimeRangeRect( (MovieTime.Zero, Session.Duration) );
		}
	}

	private void DrawTimeRangeRect( MovieTimeRange timeRange )
	{
		var startX = FromScene( Session.TimeToPixels( timeRange.Start ) ).x;
		var endX = FromScene( Session.TimeToPixels( timeRange.End ) ).x;

		Paint.DrawRect( new Rect( new Vector2( startX, LocalRect.Top ), new Vector2( endX - startX, LocalRect.Height ) ) );
	}

	private int _lastState;

	public void Frame()
	{
		var state = HashCode.Combine( Session.PixelsPerSecond, Session.TimeOffset, Session.Duration );

		if ( state != _lastState )
		{
			_lastState = state;
			Update();
		}
	}
}

public class GridItem : GraphicsItem
{
	public Session Session { get; }

	private readonly GridLines _major;
	private readonly GridLines _minor;

	private const float MajorMargin = 8f;
	private const float MinorMargin = 16f;

	public GridItem( Session session )
	{
		ZIndex = 500;
		Session = session;

		_major = new( this ) { Thickness = 2f, Position = new Vector2( 0f, MajorMargin ) };
		_minor = new( this ) { Thickness = 1f, Position = new Vector2( 0f, MinorMargin ) };
	}

	public new void Update()
	{
		_major.PrepareGeometryChange();
		_minor.PrepareGeometryChange();

		_major.Interval = Session.TimeToPixels( Session.MajorTick.Interval );
		_minor.Interval = Session.TimeToPixels( Session.MinorTick.Interval );

		_major.Size = Size - new Vector2( 0f, MajorMargin * 2f );
		_minor.Size = Size - new Vector2( 0f, MinorMargin * 2f );

		base.Update();
	}
}

public sealed class GridLines : GraphicsItem
{
	public Color Color { get; set; } = Theme.TextControl.WithAlpha( 0.02f );
	public float Thickness { get; set; } = 2f;
	public float Interval { get; set; } = 16f;

	private int? _pixmapHash;
	private Pixmap _pixmap;

	private const int PixmapHeight = 1;

	private int CalculatePixmapHash() => HashCode.Combine( Color, Thickness, (int)MathF.Round( Interval ) );

	public GridLines( GraphicsItem parent = null ) : base( parent ) { }

	private Pixmap GetPixmap()
	{
		var hash = CalculatePixmapHash();

		if ( _pixmap is { } pixmap && _pixmapHash == hash )
		{
			return pixmap;
		}

		_pixmapHash = hash;

		// Use nearest power of 2 to avoid allocating too often

		var width = Interval.NearestPowerOfTwo();

		if ( _pixmap?.Width != width )
		{
			_pixmap = new Pixmap( width, PixmapHeight );
		}

		_pixmap.Clear( Color.Transparent );

		using ( Paint.ToPixmap( _pixmap ) )
		{
			Paint.SetPen( Color, Thickness );

			// Draw line on both left and right edge so lines more than 1 px wide don't cut off

			Paint.DrawLine( 0, new Vector2( 0, PixmapHeight ) );
			Paint.DrawLine( _pixmap.Width, new Vector2( _pixmap.Width, PixmapHeight ) );
		}

		return _pixmap;
	}

	protected override void OnPaint()
	{
		var pixmap = GetPixmap();
		var offset = ToScene( Position ).x;
		var scale = Interval / pixmap.Width;

		const float margin = 32f;

		Paint.ClearPen();
		Paint.Translate( new Vector2( -offset, 0f ) );
		Paint.Scale( scale, 1f );
		Paint.SetBrush( pixmap );
		Paint.DrawRect( LocalRect with
		{
			// Add some margin to make sure whole visible width has grid lines

			Left = LocalRect.Left + offset / scale - margin,
			Right = LocalRect.Right + offset / scale + margin
		} );
	}
}
