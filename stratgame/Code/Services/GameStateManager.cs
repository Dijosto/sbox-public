using System;
using System.Threading.Tasks;
using Sandbox;
using AtlantisStrategy.API;
using AtlantisStrategy.Models;

namespace AtlantisStrategy.Services;

/// <summary>
/// Manages the game state synchronization with the server.
/// This is the ONLY class that modifies the local game state.
/// All game state comes from the server - client is read-only.
/// </summary>
public class GameStateManager
{
	private readonly CloudflareClient _client;
	private readonly WebSocketService _websocket;

	private PlayerState _currentState;
	private bool _isLoaded;
	private DateTime _lastSync = DateTime.MinValue;
	private const float AUTO_SYNC_INTERVAL = 10.0f; // Auto-sync every 10 seconds

	/// <summary>
	/// Current player state (read-only)
	/// </summary>
	public PlayerState CurrentState => _currentState;

	/// <summary>
	/// Is the game state loaded?
	/// </summary>
	public bool IsLoaded => _isLoaded;

	/// <summary>
	/// Event fired when state is updated
	/// </summary>
	public event Action<PlayerState> OnStateUpdated;

	public GameStateManager( CloudflareClient client, WebSocketService websocket )
	{
		_client = client;
		_websocket = websocket;

		// Subscribe to WebSocket events for real-time updates
		_websocket.On( "state_update", OnServerStateUpdate );
		_websocket.On( "building_complete", OnBuildingComplete );
		_websocket.On( "training_complete", OnTrainingComplete );
		_websocket.On( "march_arrived", OnMarchArrived );
		_websocket.On( "attack_incoming", OnAttackIncoming );
	}

	/// <summary>
	/// Initial load of game state from server
	/// </summary>
	public async Task<bool> LoadStateAsync()
	{
		try
		{
			Log.Info( "[GameState] Loading player state..." );

			var state = await _client.GetAsync<PlayerState>( "/api/player/state" );

			if ( state != null )
			{
				_currentState = state;
				_isLoaded = true;
				_lastSync = DateTime.Now;

				Log.Info( $"[GameState] State loaded for player: {state.PlayerName}" );

				OnStateUpdated?.Invoke( _currentState );
				return true;
			}

			Log.Error( "[GameState] Failed to load state: server returned null" );
			return false;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[GameState] Failed to load state: {ex.Message}" );
			return false;
		}
	}

	/// <summary>
	/// Force refresh state from server
	/// </summary>
	public async Task RefreshStateAsync()
	{
		await LoadStateAsync();
	}

	/// <summary>
	/// Update loop - handles auto-sync
	/// </summary>
	public async Task UpdateAsync()
	{
		if ( !_isLoaded || !_client.IsAuthenticated )
		{
			return;
		}

		// Auto-sync periodically
		var timeSinceLastSync = (float)(DateTime.Now - _lastSync).TotalSeconds;
		if ( timeSinceLastSync >= AUTO_SYNC_INTERVAL )
		{
			await RefreshStateAsync();
		}
	}

	/// <summary>
	/// Get current resource amounts (with interpolation for display)
	/// </summary>
	public Resources GetCurrentResources()
	{
		if ( !_isLoaded )
		{
			return new Resources();
		}

		// Clone current resources
		var resources = new Resources
		{
			Food = _currentState.Resources.Food,
			Wood = _currentState.Resources.Wood,
			Stone = _currentState.Resources.Stone,
			Metal = _currentState.Resources.Metal,
			Gold = _currentState.Resources.Gold,
			PremiumCurrency = _currentState.Resources.PremiumCurrency,
			FoodRate = _currentState.Resources.FoodRate,
			WoodRate = _currentState.Resources.WoodRate,
			StoneRate = _currentState.Resources.StoneRate,
			MetalRate = _currentState.Resources.MetalRate,
			GoldRate = _currentState.Resources.GoldRate,
			FoodCap = _currentState.Resources.FoodCap,
			WoodCap = _currentState.Resources.WoodCap,
			StoneCap = _currentState.Resources.StoneCap,
			MetalCap = _currentState.Resources.MetalCap,
			GoldCap = _currentState.Resources.GoldCap
		};

		// Interpolate resource production since last update
		var elapsed = (DateTime.Now - DateTimeOffset.FromUnixTimeSeconds( _currentState.LastUpdateTimestamp ).DateTime).TotalHours;

		resources.Food = Math.Min( resources.FoodCap, (int)(resources.Food + resources.FoodRate * elapsed) );
		resources.Wood = Math.Min( resources.WoodCap, (int)(resources.Wood + resources.WoodRate * elapsed) );
		resources.Stone = Math.Min( resources.StoneCap, (int)(resources.Stone + resources.StoneRate * elapsed) );
		resources.Metal = Math.Min( resources.MetalCap, (int)(resources.Metal + resources.MetalRate * elapsed) );
		resources.Gold = Math.Min( resources.GoldCap, (int)(resources.Gold + resources.GoldRate * elapsed) );

		return resources;
	}

	// Event handlers for real-time updates

	private void OnServerStateUpdate( GameEvent evt )
	{
		try
		{
			var state = evt.GetData<PlayerState>();
			_currentState = state;
			_lastSync = DateTime.Now;

			Log.Info( "[GameState] State updated from server" );
			OnStateUpdated?.Invoke( _currentState );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[GameState] Failed to process state update: {ex.Message}" );
		}
	}

	private void OnBuildingComplete( GameEvent evt )
	{
		Log.Info( "[GameState] Building completed" );

		// Trigger refresh
		_ = RefreshStateAsync();
	}

	private void OnTrainingComplete( GameEvent evt )
	{
		Log.Info( "[GameState] Training completed" );

		// Trigger refresh
		_ = RefreshStateAsync();
	}

	private void OnMarchArrived( GameEvent evt )
	{
		Log.Info( "[GameState] March arrived" );

		// Trigger refresh
		_ = RefreshStateAsync();
	}

	private void OnAttackIncoming( GameEvent evt )
	{
		var data = evt.GetData<AttackData>();
		Log.Warning( $"[GameState] Incoming attack from {data.AttackerName}! ETA: {data.ArrivalTime}" );

		// UI should show attack warning
		// Game.Current.ShowAttackWarning(data);
	}
}

/// <summary>
/// Attack notification data
/// </summary>
public class AttackData
{
	public string AttackerId { get; set; }
	public string AttackerName { get; set; }
	public long ArrivalTime { get; set; }
	public Vector2Int Origin { get; set; }
}
