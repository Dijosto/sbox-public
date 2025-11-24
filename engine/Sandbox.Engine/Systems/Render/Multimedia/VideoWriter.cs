using NativeEngine;
namespace Sandbox;

/// <summary>
/// Allows the creation of video content by encoding a sequence of frames.
/// </summary>
public sealed class VideoWriter : IDisposable
{
	public struct Config
	{
		// note: keeping file save path out of this
		// because we'll probably only want to expose that
		// carefully, and allow other write options

		public int Width;
		public int Height;
		public int FrameRate;
		public int Bitrate;
		public Codec Codec;
		public Container Container;

		/// <summary>
		/// Can this container support the codec.
		/// </summary>
		public bool IsCodecSupported()
		{
			// Validate codec support based on container
			return Container switch
			{
				Container.MP4 => Codec == Codec.H264 || Codec == Codec.H265,
				Container.WebM => Codec == Codec.VP8 || Codec == Codec.VP9,
				Container.WebP => Codec == Codec.WebP,
				_ => false,
			};
		}

		internal string CodecName => Codec switch
		{
			Codec.H264 => "h264_vulkan",
			Codec.H265 => "hevc_vulkan",
			Codec.VP8 => "libvpx",
			Codec.VP9 => "libvpx-vp9",
			Codec.WebP => "libwebp_anim",
			_ => null,
		};

		internal string ContainerName => Container.ToString().ToLower();
	}

	[Expose]
	public enum Codec
	{
		/// <summary>
		/// H.264 codec (does not support transparency)
		/// </summary>
		H264,

		/// <summary>
		/// H.265 codec (does not support transparency)
		/// Only supported on modern GPUS, will fallback to H.264 if not supported.
		/// </summary>
		H265,

		/// <summary>
		/// VP8 codec (does not support transparency)
		/// </summary>
		VP8,

		/// <summary>
		/// VP9 codec (supports transparency)
		/// </summary>
		VP9,

		/// <summary>
		/// WebP codec (supports transparency)
		/// </summary>
		WebP,
	}

	[Expose]
	public enum Container
	{
		/// <summary>
		/// MP4 container (does not support transparency)
		/// </summary>
		MP4,

		/// <summary>
		/// WebM container (supports transparency)
		/// </summary>
		WebM,

		/// <summary>
		/// WebP container (supports transparency)
		/// </summary>
		WebP,
	}

	private CVideoRecorder native;

	private readonly string path;
	private readonly int width;
	private readonly int height;
	private readonly int frameRate;
	private readonly int bitrate;

	public int Width => width;
	public int Height => height;

	internal VideoWriter( string path, Config config )
	{
		if ( !config.IsCodecSupported() )
			throw new ArgumentException( $"{config.Container} container does not support {config.Codec} codec" );

		this.path = path;

		width = config.Width;
		height = config.Height;
		frameRate = config.FrameRate > 0 ? config.FrameRate : 60;
		bitrate = config.Bitrate > 0 ? config.Bitrate : 8;

		var audioSampleRate = 44100;
		var audioChannels = 2;

		native = CVideoRecorder.Create();
		native.Initialize( path, width, height, frameRate, bitrate, audioSampleRate, audioChannels, config.CodecName );
	}

	~VideoWriter()
	{
		MainThread.QueueDispose( this );
	}

	/// <summary>
	/// Dispose this recorder, the encoder will be flushed and video finalized.
	/// </summary>
	public void Dispose()
	{
		if ( native.IsValid )
		{
			native.Destroy();
			native = IntPtr.Zero;
		}

		GC.SuppressFinalize( this );
	}


	/// <summary>
	/// Finish creating this video. The encoder will be flushed and video finalized.
	/// </summary>
	public async Task FinishAsync()
	{
		if ( !native.IsValid )
			return;

		GC.SuppressFinalize( this );

		var n = native;
		native = IntPtr.Zero;

		await Task.Run( () => n.Destroy() );
	}


	/// <summary>
	/// Add a frame of data to be encoded. Timestamp is in microseconds. 
	/// If a timestamp is not specified, it will use an incremented 
	/// frame count as the timestamp.
	/// </summary>
	/// <param name="data">The frame data to be encoded.</param>
	/// <param name="timestamp">The timestamp for the frame in microseconds. If not specified, an incremented frame count will be used.</param>
	public unsafe bool AddFrame( ReadOnlySpan<byte> data, TimeSpan? timestamp = default )
	{
		if ( !native.IsValid )
			return false;

		long mcs = (long)(timestamp?.TotalMicroseconds ?? -1);

		if ( data.Length != (width * height * 4) )
			throw new ArgumentException( $"Invalid frame data" );

		fixed ( byte* dataPtr = data )
		{
			native.AddVideoFrame( (IntPtr)dataPtr, mcs );
		}

		return true;
	}

	/// <summary>
	/// Add a frame of data to be encoded. Timestamp is in microseconds. 
	/// If a timestamp is not specified, it will use an incremented 
	/// frame count as the timestamp.
	/// </summary>
	/// <param name="bitmap">The frame data to be encoded.</param>
	/// <param name="timestamp">The timestamp for the frame in microseconds. If not specified, an incremented frame count will be used.</param>
	public unsafe bool AddFrame( Bitmap bitmap, TimeSpan? timestamp = default )
	{
		return AddFrame( bitmap.GetBuffer(), timestamp );
	}

	/// <summary>
	/// Internal for now as I have no idea, how to expose audio recording in a good way yet.
	/// </summary>
	internal void AddAudioSamples( CAudioMixDeviceBuffers buffers )
	{
		native.AddAudioSamples( buffers );
	}
}
