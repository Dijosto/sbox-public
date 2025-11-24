using Sandbox.Engine;
using Steamworks;

namespace Sandbox;

/// <summary>
/// Handles callbacks from Steam lobbies and translates them to our Global, Party or Game lobbies.
/// </summary>
internal static class SteamCallbacks
{
	internal static void InitSteamCallbacks()
	{
		SteamFriends.OnPersonaStateChange += SteamFriends_OnPersonaStateChange;
		SteamFriends.OnFriendRichPresenceUpdate += SteamFriends_OnPersonaStateChange;
		SteamFriends.OnGameRichPresenceJoinRequested += SteamFriends_OnGameRichPresenceJoinRequested;
		SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;
	}

	private static void SteamFriends_OnGameRichPresenceJoinRequested( Steamworks.Friend friend, string connectStr )
	{
		using var scope = GlobalContext.MenuScope();
		ConsoleSystem.Run( "connect", connectStr.Split( ' ' ).Last() );
	}

	private static void SteamFriends_OnGameLobbyJoinRequested( Sandbox.SteamId lobby )
	{
		using var scope = GlobalContext.MenuScope();
		ConsoleSystem.Run( "connect", lobby.Value );
	}

	private static void SteamFriends_OnPersonaStateChange( Steamworks.Friend obj )
	{
		using var scope = GlobalContext.MenuScope();
		Event.Run( "friend.change", new Sandbox.Friend( obj ) );
	}
}
