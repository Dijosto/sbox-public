using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Sandbox.Diagnostics;

//
// This migth have its uses in the future. I have doubts that GC.GetPauses
// Leaving it here incase it does.
//

internal class GarbageCollection : IDisposable
{
	private GarbageEventListener _listener;

	public GarbageCollection()
	{
		Start();
	}

	~GarbageCollection()
	{
		Dispose();
	}

	public void Dispose()
	{
		GC.SuppressFinalize( this );
		Stop();
	}

	public void Start()
	{
		if ( _listener is not null )
			return;

		_listener = new GarbageEventListener();
	}

	public void Stop()
	{
		_listener?.Dispose();
		_listener = null;
	}
}

class GarbageEventListener : EventListener
{
	private const int GCKeyword = 0x1;

	protected override void OnEventSourceCreated( EventSource eventSource )
	{
		if ( eventSource.Name.Equals( "Microsoft-Windows-DotNETRuntime" ) )
		{
			EnableEvents( eventSource, EventLevel.Informational, (EventKeywords)GCKeyword );
		}
	}

	long suspendStarted = 0;

	protected override void OnEventWritten( EventWrittenEventArgs eventData )
	{
		if ( eventData.EventName == "GCSuspendEEBegin_V1" )
		{
			suspendStarted = Stopwatch.GetTimestamp();
		}
		else if ( eventData.EventName == "GCRestartEEEnd_V1" )
		{
			var duration = Stopwatch.GetTimestamp() - suspendStarted;
			suspendStarted = 0;
			var ts = TimeSpan.FromTicks( duration );

			//if ( ts.TotalMilliseconds > 0.1f )
			{
				Log.Info( $"GC: {ts.TotalMilliseconds:0.00}ms ({GC.CollectionCount( 0 )}/{GC.CollectionCount( 1 )}/{GC.CollectionCount( 2 )} - {GC.GetTotalPauseDuration().TotalMilliseconds:0.00}ms)" );
			}
		}
	}
}
