using Microsoft.Extensions.Caching.Memory;
using Sandbox;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// We only want 1 instance of a Resource class in C# and we want that to have 1 strong handle to native.
/// So we need a WeakReference lookup everytime we get a Resource from native to match that class.
/// This way GC can work for us and free anything we're no longer using anywhere, fantastic!
/// 
/// However sometimes GC is very good at it's job and will free Resources we don't keep a strong reference to
/// in generation 0 or 1 immediately after usage. This can cause the resource to need to be loaded every frame.
/// Or worse be finalized at unpredictable times.
/// 
/// So we keep a sliding memory cache of the Resources - realistically these only need to live for an extra frame.
/// But it's probably nice to keep around for longer if they're going to be used on and off.
/// </summary>
internal static class NativeResourceCache
{
	static readonly TimeSpan SlidingExpiration = TimeSpan.FromSeconds( 30 );
	static readonly MemoryCache MemoryCache = new( new MemoryCacheOptions() { } );

	/// <summary>
	/// We still want a WeakReference cache because we might have a strong reference somewhere to a resource
	/// that has been expired from the cache. And we absolutely only want 1 instance of the resource.
	/// </summary>
	static readonly ConcurrentDictionary<long, WeakReference> WeakTable = new();

	private static Action<MemoryCache, DateTime> StartScanForExpiredItemsIfNeeded { get; } = typeof( MemoryCache )
		.GetMethod( nameof( StartScanForExpiredItemsIfNeeded ), BindingFlags.Instance | BindingFlags.NonPublic )
		.CreateDelegate<Action<MemoryCache, DateTime>>();

	internal static void Add( long key, object value )
	{
		var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration( SlidingExpiration );
		MemoryCache.Set( key, value, cacheEntryOptions );

		WeakTable.TryAdd( key, new WeakReference( value ) );
	}

	internal static bool TryGetValue<T>( long key, out T value ) where T : class
	{
		if ( MemoryCache.TryGetValue( key, out value ) )
		{
			return true;
		}

		// If we missed the Cache, check our weak refs
		if ( WeakTable.TryGetValue( key, out var weakValue ) && weakValue.Target is not null )
		{
			value = weakValue.Target as T;

			// and add it back to the cache
			Add( key, value );

			return true;
		}

		return false;
	}

	static TimeSince LastScan = 0;

	/// <summary>
	/// Ticks the underlying MemoryCache to clear expired entries
	/// </summary>
	internal static void Tick()
	{
		if ( LastScan < 30 )
			return;

		LastScan = 0;

		// MemoryCache doesn't have its own timer for clearing anything...
		// This will get rid of any expired stuff
		StartScanForExpiredItemsIfNeeded( MemoryCache, DateTime.UtcNow );
	}

	/// <summary>
	/// Clear the cache when games are closed etc. ready for a <see cref="GC.Collect()"/>
	/// </summary>
	internal static void Clear()
	{
		MemoryCache.Clear();
	}
}
