using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace Sandbox;

/// <summary>
/// A growable block of native memory with automatic pooling and safe disposal.
/// Used internally for high-performance scenarios where managed arrays are too slow.
/// Memory is allocated from the unmanaged heap and will not be moved by the GC.
/// </summary>
internal unsafe class NativeMemoryBlock : IDisposable
{
	// Pool of reusable blocks to reduce allocation overhead
	static ConcurrentQueue<NativeMemoryBlock> SharedPool = new();

	/// <summary>
	/// Gets the pointer to the allocated native memory.
	/// </summary>
	public void* Pointer { get; private set; }

	/// <summary>
	/// Gets the current size of the allocated buffer in bytes.
	/// </summary>
	public int Size { get; private set; }

	/// <summary>
	/// Tracks whether this block is currently in the shared pool.
	/// </summary>
	private bool InPool;

	/// <summary>
	/// Creates a new native memory block with the specified initial size.
	/// </summary>
	/// <param name="initialSize">Initial size in bytes</param>
	public NativeMemoryBlock( int initialSize )
	{
		Pointer = NativeMemory.Alloc( (uint)initialSize );
		Size = initialSize;
	}

	/// <summary>
	/// Gets a block from the pool or creates a new one.
	/// Blocks are automatically grown to the requested size if needed.
	/// </summary>
	/// <param name="initialSize">Minimum size needed in bytes</param>
	/// <returns>A ready-to-use memory block</returns>
	public static NativeMemoryBlock GetOrCreatePooled( int initialSize )
	{
		if ( initialSize <= 0 ) initialSize = 512;

		if ( SharedPool.TryDequeue( out var pooled ) )
		{
			pooled.InPool = false;
			pooled.Grow( initialSize );
			return pooled;
		}

		return new NativeMemoryBlock( initialSize );
	}

	/// <summary>
	/// Finalizer ensures native memory is freed even if Dispose is not called.
	/// </summary>
	~NativeMemoryBlock()
	{
		Dispose();
	}

	/// <summary>
	/// Disposes the block, either returning it to the pool or freeing the memory.
	/// Small blocks (&lt;64KB) are pooled for reuse. Larger blocks are freed immediately.
	/// </summary>
	public void Dispose()
	{
		if ( InPool )
			return;

		if ( Pointer is null )
			return;

		// Pool blocks smaller than 64KB (up to 8 in the pool)
		// Larger blocks aren't pooled to avoid memory bloat
		if ( Size < 1024 * 64 && SharedPool.Count < 8 )
		{
			SharedPool.Enqueue( this );
			InPool = true;
			return;
		}

		// Free memory for large blocks or when pool is full
		if ( Pointer is not null )
		{
			NativeMemory.Free( Pointer );
			Pointer = null;
		}
	}

	/// <summary>
	/// Grows the buffer to at least the specified size.
	/// The actual size may be larger due to exponential growth strategy (doubling).
	/// </summary>
	/// <param name="totalSize">Minimum required size in bytes</param>
	/// <exception cref="ObjectDisposedException">If the block has been disposed</exception>
	public void Grow( int totalSize )
	{
		if ( Pointer is null )
			throw new ObjectDisposedException( nameof( NativeMemoryBlock ) );

		if ( Size >= totalSize )
			return;

		// Double size until we reach the required size
		while ( Size < totalSize )
		{
			Size *= 2;
		}

		Pointer = NativeMemory.Realloc( Pointer, (uint)Size );
	}

	/// <summary>
	/// Interprets the memory as a null-terminated UTF-8 string and converts it to a C# string.
	/// </summary>
	/// <returns>The decoded string, or empty string if no null terminator found</returns>
	public string ToNullTerminatedUtf8String()
	{
		// Find null terminator
		int len = 0;
		for ( int i = 0; i < Size; i++ )
		{
			if ( ((byte*)Pointer)[i] == 0 )
			{
				len = i;
				break;
			}
		}

		if ( len == 0 || len >= Size )
			return string.Empty;

		return System.Text.Encoding.UTF8.GetString( (byte*)Pointer, len );
	}
}
