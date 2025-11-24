using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable

internal static class MovieExtensions
{
	/// <summary>
	/// Gets the <see cref="GameObject"/> that the given property is contained within.
	/// </summary>
	public static GameObject? GetTargetGameObject( this ITrackTarget property )
	{
		while ( property is ITrackProperty memberProperty )
		{
			property = memberProperty.Parent;
		}

		return property switch
		{
			ITrackReference<GameObject> goProperty => goProperty.Value,
			ITrackReference { Value: Component cmp } => cmp.GameObject,
			_ => null
		};
	}

	/// <summary>
	/// Repeats a time range with <paramref name="innerDuration"/> to fill up the given <paramref name="outerRange"/>.
	/// </summary>
	public static IEnumerable<(MovieTimeRange Range, MovieTransform Transform)> Repeat( this MovieTime innerDuration,
		MovieTimeRange outerRange )
	{
		return new MovieTimeRange( 0d, innerDuration ).Repeat( outerRange );
	}

	/// <summary>
	/// Repeats a <paramref name="innerRange"/> to fill up the given <paramref name="outerRange"/>.
	/// </summary>
	public static IEnumerable<(MovieTimeRange Range, MovieTransform Transform)> Repeat( this MovieTimeRange innerRange, MovieTimeRange outerRange )
	{
		if ( !innerRange.Duration.IsPositive )
		{
			// Avoid infinite loops

			yield break;
		}

		var firstOffset = (outerRange.Start - innerRange.Start).GetFrameIndex( innerRange.Duration ) * innerRange.Duration;
		var lastOffset = (outerRange.End - innerRange.Start).GetFrameIndex( innerRange.Duration ) * innerRange.Duration;

		for ( var offset = firstOffset; offset <= lastOffset; offset += innerRange.Duration )
		{
			yield return ((innerRange + offset).Clamp( outerRange ), new MovieTransform( offset ));
		}
	}
}
