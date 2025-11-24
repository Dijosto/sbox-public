using Native;
using System;

namespace Editor;

public class ColorSampler
{
	public Action<Color> OnPicked;
	public Action OnCancelled;

	private List<ColorSamplerOverlay> _overlays;

	public ColorSampler()
	{
		_overlays = new List<ColorSamplerOverlay>();
	}

	public void Show()
	{
		for ( int i = 0; i < QApp.ScreenCount(); i++ )
		{
			var overlay = new ColorSamplerOverlay( i );
			overlay.OnPicked += _OnPicked;
			overlay.OnCancelled += _OnCancelled;
			_overlays.Add( overlay );
		}
	}

	public void Hide()
	{
		foreach ( var overlay in _overlays )
		{
			overlay.Destroy();
		}
		_overlays.Clear();
	}

	~ColorSampler()
	{
		Hide();
	}

	private void _OnPicked( Color color )
	{
		OnPicked?.Invoke( color );
		Hide();
	}

	private void _OnCancelled()
	{
		OnCancelled?.Invoke();
		Hide();
	}
}

internal class ColorSamplerOverlay : Widget
{
	public Action<Color> OnPicked;
	public Action OnCancelled;

	private Pixmap _pixmap;

	public ColorSamplerOverlay( int screenNumber ) : base()
	{
		//PixmapCursor = Pixmap.FromFile( "toolimages:cursors/eyedropper.png" );
		Cursor = CursorShape.Blank;

		IsFramelessWindow = true;
		DeleteOnClose = true;

		QScreen screen = QApp.GetScreen( screenNumber );

		_pixmap = new Pixmap( screen.getCapture() );

		Show();
		Raise();

		Rect rect = screen.geometry().Rect;
		Position = rect.Position;
		Size = rect.Size;

		MouseTracking = true;
	}

	public Color CurrentColor()
	{
		Vector3 curPos = _widget.mapFromGlobal( Native.QApp.CursorPosition() );
		if ( curPos.x < 0 || curPos.y < 0 || curPos.x >= _pixmap.Width || curPos.y >= _pixmap.Height )
		{
			return Color.Black;
		}

		return _pixmap.GetPixel( (int)curPos.x, (int)curPos.y );
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		OnPicked?.Invoke( CurrentColor() );
		e.Accepted = true;
	}

	protected override void OnMouseRightClick( MouseEvent e )
	{
		base.OnMouseRightClick( e );

		OnCancelled?.Invoke();
		e.Accepted = true;
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		base.OnMouseMove( e );
		Update();
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.Draw( ScreenRect.WithoutPosition, _pixmap );

		Vector3 curPos = _widget.mapFromGlobal( Native.QApp.CursorPosition() );
		Vector3 nativePos = _widget.mapFromGlobal( Native.QApp.NativeCursorPosition() );
		if ( curPos.x < 0 || curPos.y < 0 || curPos.x >= _pixmap.Width || curPos.y >= _pixmap.Height )
		{
			return;
		}

		const int SAMPLE_RADIUS = 6;
		const int PREVIEW_PIXEL_SIZE = 7;

		Paint.Pen = new Color( 128, 128, 128 );
		Paint.Antialiasing = true;
		Paint.TextAntialiasing = true;

		Vector3 drawPos = curPos - (new Vector3( PREVIEW_PIXEL_SIZE, PREVIEW_PIXEL_SIZE ) / 2);
		int previewSize = PREVIEW_PIXEL_SIZE * (SAMPLE_RADIUS * 2);

		Color current = CurrentColor();
		Paint.SetBrush( current );
		Rect previreRect = new Rect( drawPos.x - (previewSize / 2), drawPos.y + (previewSize / 2) + 16, previewSize + PREVIEW_PIXEL_SIZE, 32 );
		Paint.DrawRect( previreRect, 4 );
		Paint.DrawText( previreRect, $"{current.Hex}", TextFlag.Center );

		Paint.Antialiasing = false;

		const int Y_OFFSET = 0;
		for ( int dy = -SAMPLE_RADIUS; dy <= SAMPLE_RADIUS; ++dy )
		{
			for ( int dx = -SAMPLE_RADIUS; dx <= SAMPLE_RADIUS; ++dx )
			{
				Vector2 pos = new Vector2( nativePos.x + dx, nativePos.y + dy );
				if ( pos.x < 0 || pos.y < 0 || pos.x >= _pixmap.Width || pos.y >= _pixmap.Height )
				{
					continue;
				}

				Paint.Pen = new Color( 128, 128, 128 );
				Paint.SetBrush( _pixmap.GetPixel( (int)pos.x, (int)pos.y ) );
				Paint.DrawRect( new Rect( drawPos.x + dx * PREVIEW_PIXEL_SIZE, drawPos.y + Y_OFFSET + dy * PREVIEW_PIXEL_SIZE, PREVIEW_PIXEL_SIZE, PREVIEW_PIXEL_SIZE ) );
			}
		}

		Paint.Pen = Color.Black;
		Paint.PenSize = 2;
		Paint.ClearBrush();
		Paint.DrawRect( new Rect( drawPos.x, drawPos.y + Y_OFFSET, PREVIEW_PIXEL_SIZE, PREVIEW_PIXEL_SIZE ) );
		Paint.PenSize = 1;

	}

	protected override void OnBlur( FocusChangeReason reason )
	{
		base.OnBlur( reason );
		OnCancelled?.Invoke();
	}
}
