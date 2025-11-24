using Sandbox.Utility;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using static Sandbox.Diagnostics.Performance;

namespace Sandbox.Diagnostics;

public static partial class PerformanceStats
{
	public sealed class Timings
	{
		internal static ConcurrentDictionary<string, Timings> All = new( StringComparer.OrdinalIgnoreCase );

		public static Timings Async { get; } = Get( "Async", "#e9edc9" );
		public static Timings Animation { get; } = Get( "Animation", "#ff70a6" );
		public static Timings Audio { get; } = Get( "Audio", "#bdb2ff" );
		public static Timings AudioMixingThread { get; } = Get( "AudioMixingThread", "#ff9f00" );
		public static Timings Editor { get; } = Get( "Editor", "#7f8188" );
		//	public static Timings Io { get; } = Get( "IO", "#b5838d" );
		public static Timings Input { get; } = Get( "Input", "#e9ff70" );
		//	public static Timings Internal { get; } = Get( "Internal", "#e5e5e5" );
		public static Timings NavMesh { get; } = Get( "NavMesh", "#738D45" );
		public static Timings Network { get; } = Get( "Network", "#809bce" );
		public static Timings Particles { get; } = Get( "Particles", "#f7aef8" );
		public static Timings Physics { get; } = Get( "Physics", "#f37748" );
		public static Timings Render { get; } = Get( "Render", "#8ac926" );
		public static Timings Scene { get; } = Get( "Scene", "#56cbf9" );
		public static Timings Ui { get; } = Get( "UI", "#b4869f" );
		public static Timings Video { get; } = Get( "Video", "#f5cac3" );

		/// <summary>
		/// Return a list of the main top tier timings we're interested in
		/// </summary>
		public static IEnumerable<Timings> GetMain() => _main;

		private static readonly ReadOnlyCollection<Timings> _main = new List<Timings> { Async, Animation, Audio, AudioMixingThread, Editor, Input, NavMesh, Network, Particles, Physics, Render, Scene, Ui, Video }.AsReadOnly();

		public string Name { get; internal set; }
		public Color Color { get; internal set; }

		Superluminal _superluminal;

		internal static void FlipAll()
		{
			foreach ( var a in All )
			{
				if ( a.Value.IsManualFlip )
					continue;

				a.Value.Flip();
			}
		}

		public static Timings Get( string stage, Color? color = default )
		{
			if ( All.TryGetValue( stage, out var timing ) )
				return timing;

			return All.GetOrAdd( stage, f => new Timings( stage, color ?? Color.White ) );
		}

		internal Timings( string name, Color color )
		{
			Name = name;
			Color = color;
			_superluminal = new Superluminal( Name, color );
		}

		public struct Frame
		{
			public int Calls;
			public float TotalMs;
		}

		public Sandbox.Utility.CircularBuffer<Frame> History { get; } = new( 256 );

		internal void Flip()
		{
			lock ( this )
			{
				var f = new Frame();
				f.Calls = calls;
				f.TotalMs += (float)(ticks * (1_000.0 / Stopwatch.Frequency));
				f.TotalMs += (float)milliseconds;

				History.PushFront( f );

				calls = 0;
				ticks = 0;
				milliseconds = 0;
			}
		}

		public bool IsManualFlip { get; set; }

		int calls;
		long ticks;
		double milliseconds;

		internal Performance.ScopeSection Scope()
		{
			_superluminal.Start();

			Interlocked.Increment( ref calls );

			var o = new Performance.ScopeSection()
			{
				Source = this,
				Timer = FastTimer.StartNew()
			};

			return o;
		}

		internal void ScopeFinished( ScopeSection section )
		{
			Interlocked.Add( ref ticks, section.Timer.ElapsedTicks );
			_superluminal.Dispose();
		}

		internal void AddMilliseconds( double ms, int addcalls = 1 )
		{
			milliseconds += ms;
			calls += addcalls;
		}

		public float AverageMs( int frames )
		{
			if ( History.Count() == 0 )
				return 0;

			return History.Take( frames ).Average( x => x.TotalMs );
		}

		public PeriodMetric GetMetric( int frames )
		{
			if ( History.Count() == 0 )
				return default;

			if ( frames == 1 )
			{
				var f = History.First();
				return new PeriodMetric( f.TotalMs, f.TotalMs, f.TotalMs, f.Calls );
			}
			else
			{
				var f = History.Take( frames );
				return new PeriodMetric( f.Min( x => x.TotalMs ), f.Max( x => x.TotalMs ), f.Average( x => x.TotalMs ), f.Sum( x => x.Calls ) );
			}
		}
	}

}
