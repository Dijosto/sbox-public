using BenchmarkDotNet.Attributes;
using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;


[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ByteStreamTest
{
	Guid Guid = Guid.NewGuid();
	byte[] byteBuffer = new byte[34];
	int initialBuffer = 512;

	[GlobalSetup]
	public void Setup()
	{

	}

	[Benchmark]
	public void ByteStreamInt()
	{
		using var writer = ByteStream.Create( initialBuffer );

		for ( int i = 0; i < 512; i++ )
		{
			writer.Write( i );
		}
	}

	[Benchmark]
	public void ByteStreamByte()
	{
		using var writer = ByteStream.Create( initialBuffer );

		for ( int i = 0; i < 512; i++ )
		{
			writer.Write( byteBuffer );
		}
	}

	[Benchmark]
	public void ByteStreamGuid()
	{
		using var writer = ByteStream.Create( initialBuffer );

		for ( int i = 0; i < 512; i++ )
		{
			writer.Write( Guid );
		}
	}

	[Benchmark]
	public void ByteStreamString()
	{
		using var writer = ByteStream.Create( initialBuffer );

		for ( int i = 0; i < 512; i++ )
		{
			writer.Write( "Hello there" );
		}
	}

	[Benchmark]
	public void PooledMemoryStreamInt()
	{
		var memoryStream = PooledMemoryStream.Rent( initialBuffer );
		using var writer = new BinaryWriter( memoryStream, Encoding.UTF8, true );

		for ( int i = 0; i < 512; i++ )
		{
			writer.Write( (uint)i );
		}
	}

	[Benchmark]
	public void PooledMemoryStreamByte()
	{
		var memoryStream = PooledMemoryStream.Rent( initialBuffer );
		using var writer = new BinaryWriter( memoryStream, Encoding.UTF8, true );

		for ( int i = 0; i < 512; i++ )
		{
			writer.Write( byteBuffer );
		}
	}

	[Benchmark]
	public unsafe void PooledMemoryStreamGuid()
	{
		var memoryStream = PooledMemoryStream.Rent( initialBuffer );
		using var writer = new BinaryWriter( memoryStream, Encoding.UTF8, true );

		for ( int i = 0; i < 512; i++ )
		{
			WriteGuid( writer, in Guid );
		}
	}

	[Benchmark]
	public unsafe void PooledMemoryStreamString()
	{
		var memoryStream = PooledMemoryStream.Rent( initialBuffer );
		using var writer = new BinaryWriter( memoryStream, Encoding.UTF8, true );

		for ( int i = 0; i < 512; i++ )
		{
			writer.Write( "Hello there" );
		}
	}

	private void WriteGuid( BinaryWriter writer, in Guid guid )
	{
		Span<byte> buffer = stackalloc byte[16];
		MemoryMarshal.Write( buffer, in guid );
		writer.Write( buffer );
	}

	/// <summary>
	/// A wrapper around <see cref="MemoryStream"/> used internally here to rent a pooled
	/// stream and avoid allocations where possible.
	/// </summary>
	private class PooledMemoryStream : MemoryStream
	{
		private PooledMemoryStream( int capacity ) : base( capacity )
		{
		}

		// Non-thread-safe pool queue
		private static readonly Queue<PooledMemoryStream> Pool = new();

		/// <summary>
		/// Rent a new stream from the pool or create one if none are available.
		/// </summary>
		public static PooledMemoryStream Rent( int initialSize = 8192 )
		{
			if ( !Pool.TryDequeue( out var s ) )
				return new PooledMemoryStream( initialSize );

			s.Position = 0;
			s.SetLength( 0 );

			return s;

		}

		/// <summary>
		/// Get a span of only the written portion of the buffer.
		/// </summary>
		public ReadOnlySpan<byte> GetWrittenSpan()
		{
			return new ReadOnlySpan<byte>( GetBuffer(), 0, (int)Length );
		}

		/// <summary>
		/// Return this stream to the pool and reset it.
		/// </summary>
		public void Return()
		{
			Position = 0;
			SetLength( 0 );
			Pool.Enqueue( this );
		}

		protected override void Dispose( bool disposing )
		{
			throw new InvalidOperationException( "Use Return() instead of Dispose() to recycle the stream" );
		}
	}

}
