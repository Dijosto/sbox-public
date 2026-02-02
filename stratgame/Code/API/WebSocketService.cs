using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace AtlantisStrategy.API;

/// <summary>
/// WebSocket connection for real-time notifications from server
/// Falls back to polling if WebSocket unavailable
/// </summary>
public class WebSocketService
{
	private readonly CloudflareClient _client;
	private WebSocket _socket;
	private bool _isConnected;
	private bool _isConnecting;
	private DateTime _lastPoll = DateTime.MinValue;
	private long _lastEventTimestamp;

	private Dictionary<string, List<Action<GameEvent>>> _eventHandlers = new();

	public bool IsConnected => _isConnected;
	public bool UseFallbackPolling { get; set; } = true;

	public WebSocketService( CloudflareClient client )
	{
		_client = client;
	}

	/// <summary>
	/// Connect to WebSocket server
	/// </summary>
	public async Task<bool> ConnectAsync()
	{
		if ( _isConnected || _isConnecting )
		{
			return true;
		}

		_isConnecting = true;

		try
		{
			if ( !_client.IsAuthenticated )
			{
				Log.Error( "[WebSocket] Cannot connect: not authenticated" );
				return false;
			}

			Log.Info( $"[WebSocket] Connecting to {GameConfig.WebSocketUrl}..." );

			_socket = new WebSocket();

			// Add auth token to WebSocket URL
			var url = $"{GameConfig.WebSocketUrl}?token={_client.AuthToken}";

			// Set up event handlers
			_socket.OnOpen += OnWebSocketOpen;
			_socket.OnClose += OnWebSocketClose;
			_socket.OnError += OnWebSocketError;
			_socket.OnMessage += OnWebSocketMessage;

			// Connect (this is async in s&box)
			await _socket.ConnectAsync( url );

			return true;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[WebSocket] Connection failed: {ex.Message}" );

			if ( UseFallbackPolling )
			{
				Log.Info( "[WebSocket] Will use polling fallback" );
			}

			return false;
		}
		finally
		{
			_isConnecting = false;
		}
	}

	/// <summary>
	/// Disconnect from WebSocket
	/// </summary>
	public void Disconnect()
	{
		if ( _socket != null )
		{
			_socket.Close();
			_socket = null;
		}

		_isConnected = false;
		Log.Info( "[WebSocket] Disconnected" );
	}

	/// <summary>
	/// Subscribe to a specific event type
	/// </summary>
	public void On( string eventType, Action<GameEvent> handler )
	{
		if ( !_eventHandlers.ContainsKey( eventType ) )
		{
			_eventHandlers[eventType] = new List<Action<GameEvent>>();
		}

		_eventHandlers[eventType].Add( handler );
	}

	/// <summary>
	/// Unsubscribe from an event type
	/// </summary>
	public void Off( string eventType, Action<GameEvent> handler )
	{
		if ( _eventHandlers.ContainsKey( eventType ) )
		{
			_eventHandlers[eventType].Remove( handler );
		}
	}

	/// <summary>
	/// Update loop - handles polling fallback
	/// </summary>
	public async Task UpdateAsync()
	{
		// If WebSocket is connected, we don't need to poll
		if ( _isConnected )
		{
			return;
		}

		// Fallback to polling if enabled
		if ( !UseFallbackPolling || !_client.IsAuthenticated )
		{
			return;
		}

		var timeSinceLastPoll = (float)(DateTime.Now - _lastPoll).TotalSeconds;
		if ( timeSinceLastPoll < GameConfig.PollInterval )
		{
			return;
		}

		_lastPoll = DateTime.Now;

		try
		{
			// Poll for events since last check
			var events = await _client.GetAsync<GameEventList>( $"/api/events?since={_lastEventTimestamp}" );

			if ( events?.Events != null )
			{
				foreach ( var evt in events.Events )
				{
					HandleEvent( evt );

					// Update last timestamp
					if ( evt.Timestamp > _lastEventTimestamp )
					{
						_lastEventTimestamp = evt.Timestamp;
					}
				}
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[WebSocket] Polling failed: {ex.Message}" );
		}
	}

	// WebSocket event handlers
	private void OnWebSocketOpen( WebSocket socket )
	{
		_isConnected = true;
		Log.Info( "[WebSocket] Connected" );

		// Dispatch connection event
		HandleEvent( new GameEvent
		{
			Type = "connection",
			Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			Data = JsonSerializer.Serialize( new { status = "connected" } )
		} );
	}

	private void OnWebSocketClose( WebSocket socket, int code, string reason )
	{
		_isConnected = false;
		Log.Info( $"[WebSocket] Closed: {code} - {reason}" );

		// Try to reconnect after delay
		if ( UseFallbackPolling )
		{
			Log.Info( "[WebSocket] Falling back to polling" );
		}
	}

	private void OnWebSocketError( WebSocket socket, string error )
	{
		Log.Error( $"[WebSocket] Error: {error}" );
	}

	private void OnWebSocketMessage( WebSocket socket, string message )
	{
		try
		{
			var evt = JsonSerializer.Deserialize<GameEvent>( message );
			HandleEvent( evt );

			// Update last timestamp
			if ( evt.Timestamp > _lastEventTimestamp )
			{
				_lastEventTimestamp = evt.Timestamp;
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"[WebSocket] Failed to parse message: {ex.Message}" );
		}
	}

	private void HandleEvent( GameEvent evt )
	{
		if ( GameConfig.ApiDebug )
		{
			Log.Info( $"[WebSocket] Event: {evt.Type}" );
		}

		// Call registered handlers for this event type
		if ( _eventHandlers.ContainsKey( evt.Type ) )
		{
			foreach ( var handler in _eventHandlers[evt.Type] )
			{
				try
				{
					handler( evt );
				}
				catch ( Exception ex )
				{
					Log.Error( $"[WebSocket] Event handler error: {ex.Message}" );
				}
			}
		}

		// Call wildcard handlers
		if ( _eventHandlers.ContainsKey( "*" ) )
		{
			foreach ( var handler in _eventHandlers["*"] )
			{
				try
				{
					handler( evt );
				}
				catch ( Exception ex )
				{
					Log.Error( $"[WebSocket] Wildcard handler error: {ex.Message}" );
				}
			}
		}
	}
}

/// <summary>
/// Game event from server
/// </summary>
public class GameEvent
{
	public string Type { get; set; }
	public long Timestamp { get; set; }
	public string Data { get; set; }

	public T GetData<T>()
	{
		return JsonSerializer.Deserialize<T>( Data );
	}
}

/// <summary>
/// List of game events (for polling)
/// </summary>
public class GameEventList
{
	public GameEvent[] Events { get; set; }
}
