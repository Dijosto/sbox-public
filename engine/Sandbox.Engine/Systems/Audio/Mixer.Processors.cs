using Sandbox.Utility;

namespace Sandbox.Audio;

public partial class Mixer
{
	/// <summary>
	/// Private, because we want to make this as thread safe as possible
	/// </summary>
	readonly List<AudioProcessor> _processorList = [];

	/// <summary>
	/// Add a processor to the list
	/// </summary>
	public void AddProcessor( AudioProcessor ap )
	{
		lock ( Lock )
		{
			_processorList.Add( ap );
		}
	}

	/// <summary>
	/// Add a processor to the list
	/// </summary>
	public void ClearProcessors()
	{
		lock ( Lock )
		{
			foreach ( var p in _processorList )
			{
				p.OnRemovedInternal();
			}

			_processorList.Clear();
		}
	}

	/// <summary>
	/// Add a processor to the list
	/// </summary>
	public void RemoveProcessor( AudioProcessor ap )
	{
		lock ( Lock )
		{
			ap.OnRemovedInternal();
			_processorList.Remove( ap );
		}
	}

	/// <summary>
	/// The amount of processors
	/// </summary>
	[Hide]
	public int ProcessorCount
	{
		get
		{
			lock ( Lock )
			{
				return _processorList.Count();
			}
		}
	}

	/// <summary>
	/// Get the current processor list
	/// </summary>
	public AudioProcessor[] GetProcessors()
	{
		lock ( Lock )
		{
			return _processorList.ToArray();
		}
	}

	/// <summary>
	/// Get the first processor of a specific type, or null if not found
	/// </summary>
	public T GetProcessor<T>() where T : AudioProcessor
	{
		lock ( Lock )
		{
			return _processorList.OfType<T>().FirstOrDefault();
		}
	}

	static readonly Superluminal _processors = new Superluminal( "ApplyProcessors", "#4d5e73" );

	static MultiChannelBuffer _processorBuffer;

	/// <summary>
	/// Actually apply the processors to the output buffer
	/// </summary>
	void ApplyProcessors()
	{
		using var _ = _processors.Start();

		lock ( Lock )
		{
			if ( _removedListeners != null && _removedListeners.Count > 0 )
			{
				// Remove any per listener data
				foreach ( var processor in _processorList )
				{
					processor.RemoveListeners( _removedListeners );
				}
			}

			// Apply processors to every listener
			foreach ( var listener in _usedListeners )
			{
				if ( !_outputBuffers.TryGetValue( listener, out var targetBuffer ) )
					continue;

				ApplyProcessors( targetBuffer, listener );

				// Mix into final output buffer
				_outputBuffer.MixFrom( targetBuffer, 1.0f );
			}
		}
	}

	void ApplyProcessors( MultiChannelBuffer targetBuffer, Listener listener )
	{
		foreach ( var processor in _processorList )
		{
			if ( !processor.Enabled ) continue;
			if ( processor.Mix <= 0 ) continue;
			if ( processor.TargetListener is not null && processor.TargetListener != listener ) continue;

			try
			{
				if ( _processorBuffer is not null && _processorBuffer.ChannelCount != targetBuffer.ChannelCount )
				{
					_processorBuffer.Dispose();
					_processorBuffer = null;
				}

				_processorBuffer ??= new MultiChannelBuffer( targetBuffer.ChannelCount );
				_processorBuffer.CopyFrom( targetBuffer );

				processor._listener = listener.MixTransform;
				processor.SetListener( listener );
				processor.ProcessInPlace( _processorBuffer );

				targetBuffer.Scale( 1.0f - processor.Mix );
				targetBuffer.MixFrom( _processorBuffer, processor.Mix.Clamp( 0, 1 ) );
			}
			catch ( Exception e )
			{
				Log.Warning( e, $"Exception running processor: {processor} - {e.Message}" );
			}
		}
	}
}
