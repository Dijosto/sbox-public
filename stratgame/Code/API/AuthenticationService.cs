using System;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Services;

namespace AtlantisStrategy.API;

/// <summary>
/// Handles Steam authentication and JWT token management
/// </summary>
public class AuthenticationService
{
	private readonly CloudflareClient _client;
	private bool _isAuthenticating;

	public AuthenticationService( CloudflareClient client )
	{
		_client = client;
	}

	/// <summary>
	/// Authenticate with the backend using Steam ticket
	/// </summary>
	public async Task<bool> AuthenticateAsync()
	{
		if ( _isAuthenticating )
		{
			Log.Warning( "[Auth] Authentication already in progress" );
			return false;
		}

		_isAuthenticating = true;

		try
		{
			// Get Steam authentication ticket
			Log.Info( "[Auth] Requesting Steam auth ticket..." );

			// Note: In s&box, Steam authentication is handled by the platform
			// For now, we'll use the player's Steam ID as the identifier
			var steamId = Steam.SteamId;

			if ( steamId == 0 )
			{
				Log.Error( "[Auth] Not logged into Steam" );
				return false;
			}

			Log.Info( $"[Auth] Steam ID: {steamId}" );

			// Request authentication from backend
			// In a real implementation, you'd get an actual Steam auth ticket
			// For now, we'll send the Steam ID and let the backend verify
			var authRequest = new AuthRequest
			{
				SteamId = steamId.ToString(),
				ClientVersion = "1.0.0",
				Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			var response = await _client.PostAsync<AuthResponse>( "/api/auth/steam", authRequest );

			if ( response.Success )
			{
				_client.AuthToken = response.Token;
				Log.Info( "[Auth] Authentication successful" );

				// Store player info
				Game.Current.LocalPlayer.SetPlayerInfo( response.PlayerId, response.PlayerName );

				return true;
			}
			else
			{
				Log.Error( $"[Auth] Authentication failed: {response.Error}" );
				return false;
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"[Auth] Authentication error: {ex.Message}" );
			return false;
		}
		finally
		{
			_isAuthenticating = false;
		}
	}

	/// <summary>
	/// Logout and clear authentication token
	/// </summary>
	public void Logout()
	{
		_client.AuthToken = null;
		Log.Info( "[Auth] Logged out" );
	}
}

/// <summary>
/// Authentication request payload
/// </summary>
public class AuthRequest
{
	public string SteamId { get; set; }
	public string ClientVersion { get; set; }
	public long Timestamp { get; set; }
}

/// <summary>
/// Authentication response from server
/// </summary>
public class AuthResponse
{
	public bool Success { get; set; }
	public string Token { get; set; }
	public string PlayerId { get; set; }
	public string PlayerName { get; set; }
	public string Error { get; set; }
}

/// <summary>
/// Extension methods for player info management
/// </summary>
public static class PlayerExtensions
{
	private static string _playerId;
	private static string _playerName;

	public static void SetPlayerInfo( this IClient client, string playerId, string playerName )
	{
		_playerId = playerId;
		_playerName = playerName;
	}

	public static string GetPlayerId( this IClient client )
	{
		return _playerId;
	}

	public static string GetPlayerName( this IClient client )
	{
		return _playerName ?? client?.Name ?? "Unknown";
	}
}
