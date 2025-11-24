using SkiaSharp;

namespace Sandbox;

public sealed partial class Bitmap : IDisposable, IValid
{
	private SKBitmap _bitmap;
	private SKCanvas _canvas;

	public int Width => _bitmap.Width;
	public int Height => _bitmap.Height;
	public int BytesPerPixel => _bitmap.BytesPerPixel;
	public int ByteCount => _bitmap.ByteCount;
	public Rect Rect => new( 0, 0, Width, Height );


	/// <summary>
	/// The width and height of the bitmap
	/// </summary>
	public Vector2Int Size => new( Width, Height );


	public Vector2 Center => new Vector2( Width, Height ) * 0.5f;

	public bool IsFloatingPoint { get; init; }

	public bool IsValid => _bitmap is not null && _canvas is not null;

	private const int MaxDimension = 16384;

	public Bitmap( int width, int height, bool floatingPoint = false )
	{
		if ( width <= 0 || height <= 0 )
			throw new ArgumentOutOfRangeException( "Dimensions must be positive." );

		if ( width > MaxDimension || height > MaxDimension )
			throw new ArgumentOutOfRangeException( $"Dimensions cannot exceed {MaxDimension}." );

		IsFloatingPoint = floatingPoint;
		var colorType = IsFloatingPoint ? SKColorType.RgbaF16 : SKColorType.Rgba8888;
		var info = new SKImageInfo( width, height, colorType, SKAlphaType.Unpremul );

		_bitmap = new SKBitmap( info );
		_canvas = new SKCanvas( _bitmap );
	}

	/// <summary>
	/// Used internally for resizing operations
	/// </summary>
	internal Bitmap( SKBitmap bitmap )
	{
		IsFloatingPoint = bitmap.ColorType == SKColorType.RgbaF16;
		_bitmap = bitmap;
		_canvas = new SKCanvas( _bitmap );
	}

	public void Dispose()
	{
		_canvas?.Dispose();
		_canvas = default;

		_bitmap?.Dispose();
		_bitmap = default;
	}

	/// <summary>
	/// Clears the bitmap to the specified color.
	/// </summary>
	/// <param name="color">The color to fill the bitmap with.</param>
	public void Clear( Color color )
	{
		_canvas.Clear( color.ToSkF() );
	}

	/// <summary>
	/// Retrieves the pixel data of the bitmap as an array of colors.
	/// </summary>
	public Color[] GetPixels()
	{
		if ( IsFloatingPoint )
		{
			unsafe
			{
				int pixelCount = _bitmap.Width * _bitmap.Height;
				var raw = new Span<Color.Rgba16>( (void*)_bitmap.GetPixels(), pixelCount );

				// Allocate the final Color array
				var colors = new Color[pixelCount];

				// Convert each HalfColor to Color
				for ( int i = 0; i < pixelCount; i++ )
				{
					colors[i] = raw[i].ToColor();
				}

				return colors;
			}
		}
		else
		{
			return _bitmap.Pixels.Select( p => p.FromSk() ).ToArray();
		}
	}

	/// <summary>
	/// Retrieves the pixel data of the bitmap as an array of colors.
	/// </summary>
	public Color.Rgba16[] GetPixels16()
	{
		if ( IsFloatingPoint )
		{
			unsafe
			{
				int pixelCount = _bitmap.Width * _bitmap.Height;
				var raw = new Span<Color.Rgba16>( (void*)_bitmap.GetPixels(), pixelCount );
				return raw.ToArray();
			}
		}
		else
		{
			return _bitmap.Pixels.Select( p => (Color.Rgba16)p.FromSk() ).ToArray();
		}
	}

	/// <summary>
	/// Retrieves the pixel data of the bitmap as an array of colors.
	/// </summary>
	public Color32[] GetPixels32()
	{
		if ( IsFloatingPoint )
		{
			unsafe
			{
				int pixelCount = _bitmap.Width * _bitmap.Height;
				var raw = new Span<Color.Rgba16>( (void*)_bitmap.GetPixels(), pixelCount );

				// Allocate the final Color array
				var colors = new Color32[pixelCount];

				// Convert each HalfColor to Color
				for ( int i = 0; i < pixelCount; i++ )
				{
					colors[i] = raw[i].ToColor();
				}

				return colors;
			}
		}
		else
		{
			return _bitmap.Pixels.Select( p => (Color32)p.FromSk() ).ToArray();
		}
	}

	public void SetPixels( Color[] colors )
	{
		if ( colors is null || colors.Length != _bitmap.Width * _bitmap.Height )
		{
			throw new ArgumentException( "Colors array must match the size of the bitmap." );
		}

		if ( IsFloatingPoint )
		{
			unsafe
			{
				int pixelCount = _bitmap.Width * _bitmap.Height;
				var rawPixels = new Span<Color.Rgba16>( (void*)_bitmap.GetPixels(), pixelCount );
				for ( int i = 0; i < pixelCount; i++ )
				{
					rawPixels[i] = new Color.Rgba16( colors[i] );
				}
			}
		}
		else
		{
			var skColors = new SKColor[colors.Length];
			for ( int i = 0; i < colors.Length; i++ )
			{
				skColors[i] = colors[i].ToSk();
			}
			_bitmap.Pixels = skColors;
		}
	}

	/// <summary>
	/// Retrieves the color of a specific pixel in the bitmap.
	/// </summary>
	/// <param name="x">The x-coordinate of the pixel.</param>
	/// <param name="y">The y-coordinate of the pixel.</param>
	/// <returns>The color of the pixel at the specified coordinates.</returns>
	public Color GetPixel( int x, int y )
	{
		AssertBounds( x, y, 1, 1 );

		return _bitmap.GetPixel( x, y ).FromSk();
	}

	/// <summary>
	/// Sets the color of a specific pixel in the bitmap.
	/// </summary>
	/// <param name="x">The x-coordinate of the pixel.</param>
	/// <param name="y">The y-coordinate of the pixel.</param>
	/// <param name="color">The color to set the pixel to.</param>
	public void SetPixel( int x, int y, Color color )
	{
		AssertBounds( x, y, 1, 1 );

		if ( _bitmap.ColorType == SKColorType.RgbaF16 )
		{
			unsafe
			{
				int index = y * _bitmap.Width + x;
				var rawPixels = new Span<Color.Rgba16>( (void*)_bitmap.GetPixels(), _bitmap.Width * _bitmap.Height );
				rawPixels[index] = new Color.Rgba16( color );
			}
		}
		else
		{
			_bitmap.SetPixel( x, y, color.ToSk() );
		}
	}

	/// <summary>
	/// Low level, get a span of the bitmap data.
	/// </summary>
	internal unsafe Span<byte> GetBuffer()
	{
		return new Span<byte>( (void*)_bitmap.GetPixels(), ByteCount );
	}

	/// <summary>
	/// Super low level, get a pointer to the bitmap data.
	/// </summary>
	internal unsafe void* GetPointer()
	{
		return (void*)_bitmap.GetPixels();
	}

	/// <summary>
	/// Asserts that the specified region is within the bounds of the bitmap.
	/// Throws an exception if the bounds are out of range.
	/// </summary>
	/// <param name="x">The x-coordinate of the starting point.</param>
	/// <param name="y">The y-coordinate of the starting point.</param>
	/// <param name="width">The width of the region to check.</param>
	/// <param name="height">The height of the region to check.</param>
	private void AssertBounds( int x, int y, int width, int height )
	{
		if ( x < 0 || y < 0 || x + width > _bitmap.Width || y + height > _bitmap.Height )
		{
			throw new ArgumentOutOfRangeException( nameof( x ), "Specified region is out of bounds." );
		}
	}

	/// <summary>
	/// Copy the bitmap to a new one without any changes.
	/// </summary>
	public Bitmap Clone()
	{
		var newBitmap = _bitmap.Copy();
		return new Bitmap( newBitmap );
	}


	/// <summary>
	/// Returns true if this bitmap is completely opaque (no alpha)
	/// This does a pixel by pixel search, so it's not the fastest.
	/// </summary>
	public unsafe bool IsOpaque()
	{
		if ( _bitmap.AlphaType == SKAlphaType.Opaque )
			return true;

		int height = _bitmap.Height;
		int width = _bitmap.Width;
		int rowBytes = _bitmap.RowBytes;
		IntPtr pixels = _bitmap.GetPixels();

		if ( _bitmap.ColorType == SKColorType.RgbaF16 )
		{
			float* ptr = (float*)pixels;
			bool opaque = true;

			Parallel.For( 0, height, ( y, state ) =>
			{
				float* row = ptr + (y * (rowBytes / sizeof( float )));

				for ( int x = 0; x < width; x++ )
				{
					float alpha = row[x * 4 + 3]; // Alpha is the 4th channel
					if ( alpha < 1.0f ) // Check if alpha is not fully opaque
					{
						opaque = false;
						state.Stop(); // Stop all threads
						return;
					}
				}
			} );

			return opaque;

		}

		if ( _bitmap.ColorType == SKColorType.Rgba8888 )
		{
			bool opaque = true;

			Parallel.For( 0, height, ( y, state ) =>
			{
				byte* ptr = (byte*)pixels + (y * rowBytes);

				for ( int x = 0; x < width; x++ )
				{
					if ( ptr[(x * 4) + 3] != 255 )
					{
						opaque = false;
						state.Stop(); // Stop all threads
						return;
					}
				}
			} );

			return opaque;
		}

		// we can't determin that it's opaque so say it's not
		return false;
	}
}
