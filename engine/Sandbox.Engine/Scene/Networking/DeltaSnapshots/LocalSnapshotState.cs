using Sandbox.Engine;
using Sandbox.Hashing;

namespace Sandbox.Network;

/// <summary>
/// Represents the current local snapshot state for a networked object. This will contain entries that will
/// be sent to other clients.
/// </summary>
internal class LocalSnapshotState
{
	internal class Entry
	{
		public readonly HashSet<Guid> Connections = [];
		public int Slot;
		public byte[] Value;
		public ulong Hash;
	}

	public readonly List<Entry> Entries = new( 128 );
	public readonly Dictionary<int, Entry> Lookup = new( 128 );
	public readonly HashSet<Guid> UpdatedConnections = new( 128 );

	public ushort SnapshotId { get; set; }
	public ushort Version { get; set; }
	public Guid ObjectId { get; set; }
	public int Size { get; private set; }

	/// <summary>
	/// Remove a connection from stored state acknowledgements.
	/// </summary>
	/// <param name="id"></param>
	public void RemoveConnection( Guid id )
	{
		UpdatedConnections.Remove( id );

		foreach ( var entry in Entries )
		{
			entry.Connections.Remove( id );
		}
	}

	/// <summary>
	/// Clear all connections from stored state acknowledgements.
	/// </summary>
	public void ClearConnections()
	{
		UpdatedConnections.Clear();

		foreach ( var entry in Entries )
		{
			entry.Connections.Clear();
		}
	}

	/// <summary>
	/// Add a serialized byte array value to the specified slot.
	/// </summary>
	/// <param name="slot"></param>
	/// <param name="value"></param>
	public void AddSerialized( int slot, byte[] value )
	{
		var hash = XxHash3.HashToUInt64( value );

		if ( Lookup.TryGetValue( slot, out var entry ) )
		{
			if ( entry.Hash == hash )
				return;

			Size -= entry.Value.Length;

			entry.Hash = hash;
			entry.Value = value;
		}
		else
		{
			entry = new Entry
			{
				Slot = slot,
				Value = value,
				Hash = hash
			};

			Entries.Add( entry );
			Lookup[slot] = entry;
		}

		entry.Connections.Clear();
		UpdatedConnections.Clear();

		Size += value.Length;
	}

	/// <summary>
	/// Add from a <see cref="SnapshotValueCache"/> cache.
	/// </summary>
	public void AddCached<T>( SnapshotValueCache cache, int slot, T value )
	{
		AddSerialized( slot, cache.GetCached( slot, value ) );
	}
}
