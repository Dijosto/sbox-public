using Sandbox;

namespace AtlantisStrategy;

/// <summary>
/// Global configuration for the game client
/// </summary>
public static class GameConfig
{
	/// <summary>
	/// Base URL for the Cloudflare Workers API
	/// Set via console command: game.api_url "https://api.yourgame.workers.dev"
	/// </summary>
	[ConVar("game.api_url")]
	public static string ApiBaseUrl { get; set; } = "http://localhost:8787";

	/// <summary>
	/// Enable debug logging for API calls
	/// </summary>
	[ConVar("game.api_debug")]
	public static bool ApiDebug { get; set; } = true;

	/// <summary>
	/// WebSocket URL for real-time notifications
	/// </summary>
	[ConVar("game.ws_url")]
	public static string WebSocketUrl { get; set; } = "ws://localhost:8787/ws";

	/// <summary>
	/// Polling interval in seconds when WebSocket is unavailable
	/// </summary>
	[ConVar("game.poll_interval")]
	public static float PollInterval { get; set; } = 5.0f;

	/// <summary>
	/// Request timeout in seconds
	/// </summary>
	[ConVar("game.api_timeout")]
	public static float ApiTimeout { get; set; } = 30.0f;

	/// <summary>
	/// Maximum retry attempts for failed requests
	/// </summary>
	[ConVar("game.api_retries")]
	public static int MaxRetries { get; set; } = 3;
}
