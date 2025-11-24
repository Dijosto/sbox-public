namespace Sandbox.Network;

/// <summary>
/// A mock channel. Allows passing this to RPCs when they're being called locally.
/// </summary>
internal class LocalConnection : Connection
{
	public override string Address => "local";
	public override string Name => "local";
	public override bool IsHost => Networking.System?.IsHost ?? true;

	internal override void InternalClose( int closeCode, string closeReason ) { }
	internal override void InternalRecv( NetworkSystem.MessageHandler handler ) { }
	internal override void InternalSend( ByteStream stream, NetFlags flags ) { }

	public LocalConnection( Guid id )
	{
		Id = id;
	}
}

/// <summary>
/// A mock channel. Allows passing this to RPCs when they're being called locally. Mock connections
/// will also exist for other clients when connected to a dedicated server. If we try to send a message
/// to one, we'll route that message through the server instead.
/// </summary>
internal class MockConnection : Connection
{
	public override string Address => "";
	public override string Name => $"{Id}";
	public override bool IsHost => false;

	internal override void InternalClose( int closeCode, string closeReason ) { }
	internal override void InternalRecv( NetworkSystem.MessageHandler handler ) { }
	internal override void InternalSend( ByteStream stream, NetFlags flags ) { }

	internal override void SendRawMessage( ByteStream stream, NetFlags flags = NetFlags.Reliable )
	{
		// If we're a mock connection - we don't have a direct connection. We're probably
		// on a dedicated server, so let's route through the host.

		var availableHost = Host;

		if ( availableHost is null or MockConnection )
		{
			if ( Networking.Debug )
			{
				Log.Warning( "MockConnection.SendRawMessage: no available host to route through!" );
			}

			return;
		}

		var wrapper = new TargetedMessage
		{
			SenderId = Local.Id,
			TargetId = Id,
			Message = stream.ToArray(),
			Flags = (byte)flags
		};

		availableHost.SendMessage( wrapper, flags );
	}

	public MockConnection( Guid id )
	{
		Id = id;
	}
}
