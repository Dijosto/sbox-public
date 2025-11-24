
using System.Text.Json.Serialization;

namespace Sandbox;

internal record struct Line2D( Vector2 Start, Vector2 End )
{
	[JsonIgnore, Hide]
	public readonly float Length => (Start - End).Length;

	[JsonIgnore, Hide]
	public readonly float LengthSquared => (Start - End).LengthSquared;

	[JsonIgnore, Hide]
	public readonly bool IsNearZeroLength => (Start - End).IsNearZeroLength;

	[JsonIgnore, Hide]
	public readonly float AngleRadians => MathF.Atan2( End.y - Start.y, End.x - Start.x );

	[JsonIgnore, Hide]
	public readonly float AngleDegrees => AngleRadians.RadianToDegree();

	[JsonIgnore, Hide]
	public readonly Vector2 Tangent => (End - Start).Normal;

	[JsonIgnore, Hide]
	public readonly Vector2 Center => (Start + End) * 0.5f;

	public static Line2D operator +( Line2D line, Vector2 offset )
	{
		return new Line2D( line.Start + offset, line.End + offset );
	}

	public static Line2D operator -( Line2D line, Vector2 offset )
	{
		return new Line2D( line.Start - offset, line.End - offset );
	}

	/// <summary>
	/// Returns this line, clamped within a rectangle. Null if line is fully
	/// outside the rectangle.
	/// </summary>
	public readonly Line2D? Clip( Rect bounds )
	{
		Line2D? line = this;

		line = line?.Clip( new Vector2( 1f, 0f ), bounds.Left );
		line = line?.Clip( new Vector2( -1f, 0f ), -bounds.Right );
		line = line?.Clip( new Vector2( 0f, 1f ), bounds.Top );
		line = line?.Clip( new Vector2( 0f, -1f ), -bounds.Bottom );

		return line;
	}

	/// <summary>
	/// Returns this line, clamped on the positive side of a half-plane. Null if
	/// line is fully clipped.
	/// </summary>
	public readonly Line2D? Clip( Vector2 normal, float distance )
	{
		var startDot = Vector2.Dot( Start, normal ) - distance;
		var endDot = Vector2.Dot( End, normal ) - distance;

		// Fully on positive side

		if ( startDot >= 0f && endDot >= 0f )
		{
			return this;
		}

		// Fully on negative side

		if ( startDot < 0f && endDot < 0f )
		{
			return null;
		}

		var t = -startDot / (endDot - startDot);
		var clipped = Start + (End - Start) * t;

		return startDot < 0f
			? this with { Start = clipped }
			: this with { End = clipped };
	}

	/// <summary>
	/// Remaps from one range to another.
	/// </summary>
	public readonly Line2D Remap( Rect oldRange, Rect newRange )
	{
		return new Line2D(
			Start.Remap( oldRange, newRange, false ),
			End.Remap( oldRange, newRange, false ) );
	}

	/// <summary>
	/// Remaps from one range to another, clipping along the way.
	/// </summary>
	public readonly Line2D? RemapClip( Rect oldRange, Rect newRange )
	{
		return Clip( oldRange )?.Remap( oldRange, newRange );
	}

	/// <summary>
	/// Returns closest point on this line to the given point.
	/// </summary>
	public readonly Vector2 ClosestPoint( Vector2 pos )
	{
		var delta = End - Start;
		var length = delta.Length;
		var direction = delta / length;

		return Start + Vector2.Dot( pos - Start, direction ).Clamp( 0, length ) * direction;
	}

	/// <summary>
	/// Returns closest distance from this line to given point.
	/// </summary>
	public readonly float Distance( Vector2 pos )
	{
		return (pos - ClosestPoint( pos )).Length;
	}

	/// <summary>
	/// Returns closest distance from this line to given point.
	/// </summary>
	public readonly float Distance( Vector2 pos, out Vector2 closestPoint )
	{
		closestPoint = ClosestPoint( pos );
		return (pos - closestPoint).Length;
	}

	/// <summary>
	/// Returns a line along the same tangent with the given length.
	/// </summary>
	public readonly Line2D WithLength( float length, float pivot = 0.5f )
	{
		var along = Tangent * length;
		var pos = Start + (End - Start) * pivot;

		return new Line2D( pos - along * pivot, pos + along * (1f - pivot) );
	}
}
