namespace Sandbox.Internal;

public partial class TypeLibrary
{
	private readonly Dictionary<int, object> _cacheDictionary = new();

	/// <summary>
	/// Get or cache the result
	/// </summary>
	T Cached<T>( int key, Func<T> factory )
	{
		if ( _cacheDictionary.TryGetValue( key, out var obj ) && obj is T cached )
			return cached;

		var val = factory();
		_cacheDictionary[key] = val;
		return val;
	}

	/// <summary>
	/// Clear the cache. Should be called when types are added or removed.
	/// </summary>
	void InvalidateCache()
	{
		_cacheDictionary.Clear();
	}
}
