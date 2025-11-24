using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using Sandbox.MovieMaker;
using Sandbox.MovieMaker.Compiled;

namespace Editor.MovieMaker;

#nullable enable

/// <summary>
/// A <see cref="ITrackBlock"/> that has hints for UI painting.
/// </summary>
public interface IPaintHintBlock : ITrackBlock
{
	/// <summary>
	/// Gets time regions, within <paramref name="timeRange"/>, that have constantly changing values.
	/// </summary>
	IEnumerable<MovieTimeRange> GetPaintHints( MovieTimeRange timeRange );
}

/// <summary>
/// A <see cref="ITrackBlock"/> that can change dynamically, usually for previewing edits / live recordings.
/// </summary>
public interface IDynamicBlock : ITrackBlock
{
	event Action<MovieTimeRange>? Changed;
}

/// <summary>
/// A <see cref="IPropertyBlock"/> that can be added to a <see cref="IProjectPropertyTrack"/>.
/// </summary>
public interface IProjectPropertyBlock : IPropertyBlock, IPaintHintBlock
{
	IProjectPropertyBlock? Slice( MovieTimeRange timeRange );
	IProjectPropertyBlock Shift( MovieTime offset );
	IProjectPropertyBlock WithSignal( PropertySignal signal );

	PropertySignal Signal { get; }
}

public static class PropertyBlock
{
	public static IProjectPropertyBlock FromSignal( PropertySignal signal, MovieTimeRange timeRange )
	{
		var propertyType = signal.PropertyType;
		var blockType = typeof(PropertyBlock<>).MakeGenericType( propertyType );

		return (IProjectPropertyBlock)Activator.CreateInstance( blockType, signal, timeRange )!;
	}
}

public sealed partial record PropertyBlock<T>( [property: JsonPropertyOrder( 100 )] PropertySignal<T> Signal, MovieTimeRange TimeRange )
	: IPropertyBlock<T>, IProjectPropertyBlock
{
	public T GetValue( MovieTime time ) => Signal.GetValue( time.Clamp( TimeRange ) );

	public IEnumerable<MovieTimeRange> GetPaintHints( MovieTimeRange timeRange ) =>
		Signal.GetPaintHints( timeRange.Clamp( TimeRange ) );

	public PropertyBlock<T>? Slice( MovieTimeRange timeRange )
	{
		if ( timeRange == TimeRange ) return this;

		if ( timeRange.Intersect( TimeRange ) is not { } intersection )
		{
			return null;
		}

		return new PropertyBlock<T>( Signal.Reduce( intersection ), intersection );
	}

	IProjectPropertyBlock? IProjectPropertyBlock.Slice( MovieTimeRange timeRange ) => Slice( timeRange );
	IProjectPropertyBlock IProjectPropertyBlock.Shift( MovieTime offset ) => new MovieTransform( offset ) * this;

	public IProjectPropertyBlock WithSignal( PropertySignal signal ) => this with { Signal = (PropertySignal<T>)signal };

	PropertySignal IProjectPropertyBlock.Signal => Signal;

	private readonly record struct SampleSpan( int Start, int Count, bool IsConstant );

	public IEnumerable<ICompiledPropertyBlock<T>> Compile( ProjectPropertyTrack<T> track )
	{
		var sampleRate = track.Project.SampleRate;
		var samples = Signal.Sample( TimeRange, sampleRate );

		var sampleSpans = new List<SampleSpan>();

		FindConstantSpans( sampleSpans, samples );

		// If we have this many identical samples in a row, just make it a constant block.
		// Let's have a lower threshold for types that can't interpolate, like strings or ints

		var canInterpolate = Interpolator.GetDefault<T>() is not null;
		var minConstBlockSampleCount = canInterpolate
			? Math.Max( sampleRate / 2, 10 )
			: Math.Max( sampleRate / 4, 1 );

		// We take an extra sample at the end so we can interpolate smoothly to the next span

		var trailingExtraSamples = canInterpolate ? 1 : 0;

		MergeSpans( sampleSpans, minConstBlockSampleCount );

		foreach ( var span in sampleSpans )
		{
			var startTime = TimeRange.Start + MovieTime.FromFrames( span.Start, sampleRate );
			var endTime = TimeRange.Start + MovieTime.FromFrames( span.Start + span.Count, sampleRate );

			if ( span.IsConstant )
			{
				yield return new CompiledConstantBlock<T>( (startTime, endTime), samples[span.Start] );
				continue;
			}

			var spanSamples = samples.Skip( span.Start ).Take( span.Count + trailingExtraSamples );

			yield return new CompiledSampleBlock<T>( (startTime, endTime), 0d, sampleRate, [..spanSamples] );
		}
	}

	/// <summary>
	/// Appends all the ranges of <paramref name="samples"/> that have a constant value to <paramref name="sampleSpans"/>.
	/// </summary>
	private static void FindConstantSpans( List<SampleSpan> sampleSpans, IReadOnlyList<T> samples )
	{
		var comparer = EqualityComparer<T>.Default;

		var currentSpanStart = 0;
		var prevSample = samples[0];

		for ( var i = 1; i < samples.Count; i++ )
		{
			var sample = samples[i];

			if ( comparer.Equals( prevSample, sample ) ) continue;

			sampleSpans.Add( new SampleSpan( currentSpanStart, i - currentSpanStart, true ) );

			currentSpanStart = i;
			prevSample = sample;
		}

		sampleSpans.Add( new SampleSpan( currentSpanStart, samples.Count - currentSpanStart, true ) );
	}

	/// <summary>
	/// Merge sample spans that are less than <paramref name="minConstSampleCount"/>, marking them as non-constant.
	/// </summary>
	private static void MergeSpans( List<SampleSpan> sampleSpans, int minConstSampleCount )
	{
		if ( minConstSampleCount < 2 ) return;

		for ( var i = sampleSpans.Count - 2; i >= 0; i-- )
		{
			var prev = sampleSpans[i];
			var next = sampleSpans[i + 1];

			if ( prev.IsConstant && prev.Count >= minConstSampleCount ) continue;
			if ( next.IsConstant && next.Count >= minConstSampleCount ) continue;

			sampleSpans.RemoveAt( i + 1 );
			sampleSpans[i] = new SampleSpan( prev.Start, prev.Count + next.Count, false );
		}
	}
}
