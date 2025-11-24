using Sandbox;

/// <summary>
/// You're not seeing things, QT uses fucking doubles
/// </summary>
internal struct QRectF
{
	public double x;
	public double y;
	public double w;
	public double h;

	public static implicit operator QRectF( in Rect value )
	{
		return new QRectF { x = (int)value.Left, y = (int)value.Top, w = (int)value.Width, h = (int)value.Height };
	}

	public readonly Rect Rect => new( (float)x, (float)y, (float)w, (float)h );

}
