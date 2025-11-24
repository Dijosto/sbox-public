using System.Collections.Immutable;

namespace Sandbox.UI;

[SkipHotload]
internal struct GradientInfo
{

	public float Angle;
	public Length OffsetX;
	public Length OffsetY;
	public RadialSizeMode SizeMode;
	public GradientTypes GradientType;
	public ImmutableArray<Styles.GradientColorOffset> ColorOffsets;

	public void CopyFrom( GradientInfo other )
	{
		ColorOffsets = ImmutableArray<Styles.GradientColorOffset>.Empty;

		if ( !other.ColorOffsets.IsDefaultOrEmpty )
		{
			ColorOffsets = ColorOffsets.AddRange( other.ColorOffsets );
		}

		Angle = other.Angle;
		SizeMode = other.SizeMode;
		OffsetX = other.OffsetX;
		OffsetY = other.OffsetY;
		GradientType = other.GradientType;
	}

	public void AddFrom( GradientInfo other )
	{
		if ( other.ColorOffsets.IsDefaultOrEmpty )
			return;

		CopyFrom( other );
	}

	public override int GetHashCode()
	{
		if ( ColorOffsets.IsDefaultOrEmpty )
			return 0;

		return HashCode.Combine( Angle, SizeMode, OffsetX, OffsetY, GradientType, ColorOffsets );
	}

	public enum RadialSizeMode
	{
		FarthestSide = 0,
		FarthestCorner = 1,
		ClosestSide = 2,
		ClosestCorner = 3,
		Circle = 4
	}

	public enum GradientTypes
	{
		Linear = 0,
		Radial = 1
	}

}
