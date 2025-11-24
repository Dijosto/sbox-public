using System;
using System.Collections.Generic;
using Sandbox.Internal;
using Sandbox.Network;

namespace Sandbox.SceneTests;

#nullable enable

internal sealed class TestConnection : Connection
{
	public record struct Message( InternalMessageType Type, object? Payload = null );

	public List<Message> Messages { get; } = new();

	internal override void InternalSend( ByteStream stream, NetFlags flags )
	{
		var reader = new ByteStream( stream.ToArray() );

		var type = reader.Read<InternalMessageType>();

		switch ( type )
		{
			case InternalMessageType.Chunk:
				throw new NotImplementedException();

			case InternalMessageType.Packed:
				Messages.Add( new Message( type, GlobalGameNamespace.TypeLibrary.FromBytes<object>( ref reader ) ) );
				break;

			default:
				Messages.Add( new Message( type ) );
				break;
		}
	}

	internal override void InternalRecv( NetworkSystem.MessageHandler handler )
	{

	}

	internal override void InternalClose( int closeCode, string closeReason )
	{

	}
}
