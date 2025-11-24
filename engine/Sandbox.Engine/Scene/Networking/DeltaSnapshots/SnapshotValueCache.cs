using Sandbox.Engine;

namespace Sandbox.Network;

internal class SnapshotValueCache
{
	private Dictionary<int, byte[]> Serialized { get; } = new();
	private Dictionary<int, object> Cache { get; } = new();

	/// <summary>
	/// Get cached bytes from the specified value if they exist. If the value is different
	/// then re-serialize and cache again.
	/// </summary>
	public byte[] GetCached<T>( int slot, T value )
	{
		if ( Cache.TryGetValue( slot, out var cached ) && Equals( cached, value ) )
			return Serialized[slot];

		var bytes = GlobalContext.Current.TypeLibrary.ToBytes( value );
		Serialized[slot] = bytes;
		Cache[slot] = value;

		return bytes;
	}

	public void Remove( int slot )
	{
		Serialized.Remove( slot );
		Cache.Remove( slot );
	}

	public void Clear()
	{
		Serialized.Clear();
		Cache.Clear();
	}
}
