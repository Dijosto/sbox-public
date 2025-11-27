using Sandbox.Network;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Editor;

public static partial class EditorUtility
{
	public static partial class Network
	{
		/// <summary>
		/// True if the network system is active
		/// </summary>
		public static bool Active => Networking.System is not null;

		/// <summary>
		/// True if the network system is active and we're the host
		/// </summary>
		public static bool Hosting => Networking.System?.IsHost ?? false;

		/// <summary>
		/// True if the network system is active and we're the host
		/// </summary>
		public static bool Client => Networking.System?.IsClient ?? false;

		/// <summary>
		/// Determines who can join a lobby hosted from the editor. Should only be set
		/// before creating a lobby. Persists between lobbies.
		/// </summary>
		public static LobbyPrivacy HostPrivacy
		{
			get => Networking.EditorLobbyPrivacy;
			set => Networking.EditorLobbyPrivacy = value;
		}

		/// <summary>
		/// Disconnect from the current network session
		/// </summary>
		public static void Disconnect() => Networking.Disconnect();

		/// <summary>
		/// Connenct to a network address
		/// </summary>
		public static void Connect( string address ) => Networking.Connect( address );

		/// <summary>
		/// Query available lobbies for a specific game.
		/// </summary>
		public static Task<List<LobbyInformation>> QueryLobbies( string gameIdent, CancellationToken ct = default )
			=> Networking.QueryLobbies( gameIdent, ct );

		/// <summary>
		/// Query available lobbies with custom filters.
		/// </summary>
		public static Task<List<LobbyInformation>> QueryLobbies( Dictionary<string, string> filters, bool includeServers = true, CancellationToken ct = default )
			=> Networking.QueryLobbies( filters, includeServers, ct );

		/// <summary>
		/// Try to join a lobby by its Steam ID. Returns true if connection was successful.
		/// This allows the editor to join normal s&box games on the platform.
		/// </summary>
		public static async Task<bool> JoinLobby( ulong lobbyId )
		{
			if ( !Game.IsPlaying )
			{
				EditorScene.Play();
			}

			return await Networking.TryConnectSteamId( lobbyId );
		}

		/// <summary>
		/// Try to join any available lobby for the specified game. Returns true if joined successfully.
		/// </summary>
		public static async Task<bool> JoinBestLobby( string gameIdent )
		{
			if ( !Game.IsPlaying )
			{
				EditorScene.Play();
			}

			return await Networking.JoinBestLobby( gameIdent );
		}

		/// <summary>
		/// Start hosting a lobby. If we're not already in play mode, we'll enter play mode first.
		/// </summary>
		public static void StartHosting()
		{
			if ( !Game.IsPlaying )
			{
				EditorScene.Play();
			}

			Networking.CreateLobby( new LobbyConfig() );
		}

		/// <summary>
		/// Return all of the channels on this connection. 
		/// If you're the host, it should return all client connections.
		/// If you're the client, it should return empty - unless you're in a p2p session (lobby).
		/// Returns empty if you're not connected
		/// </summary>
		public static Connection[] Channels
		{
			get
			{
				if ( Networking.System is null ) return Array.Empty<Connection>();

				return Networking.System.Connections.ToArray();
			}
		}

		/// <summary>
		/// Return all of the sockets on this connection. 
		/// If you're the host, it should return all active sockets.
		/// If you're the client, it should return empty - unless you're in a p2p session (lobby).
		/// Returns empty if you're not connected
		/// </summary>
		public static NetworkSocket[] Sockets
		{
			get
			{
				if ( Networking.System is null ) return Array.Empty<NetworkSocket>();

				return Networking.System.Sockets.ToArray();
			}
		}

	}
}
