using System.Threading.Tasks;
using Sandbox;
using AtlantisStrategy.API;
using AtlantisStrategy.Services;

namespace AtlantisStrategy;

/// <summary>
/// Main game controller - entry point for the strategy game
/// </summary>
public partial class Game : GameManager
{
	public static Game Current { get; private set; }

	// Core services
	private CloudflareClient _apiClient;
	private AuthenticationService _authService;
	private WebSocketService _websocketService;
	private GameStateManager _stateManager;
	private GameActionsService _actionsService;

	// Public accessors
	public CloudflareClient ApiClient => _apiClient;
	public GameStateManager StateManager => _stateManager;
	public GameActionsService Actions => _actionsService;
	public IClient LocalPlayer => Game.LocalClient;

	private bool _isInitialized;
	private bool _isAuthenticating;

	public Game()
	{
		Current = this;

		if ( Game.IsClient )
		{
			// Initialize services
			_apiClient = new CloudflareClient();
			_authService = new AuthenticationService( _apiClient );
			_websocketService = new WebSocketService( _apiClient );
			_stateManager = new GameStateManager( _apiClient, _websocketService );
			_actionsService = new GameActionsService( _apiClient, _stateManager );

			Log.Info( "[Game] Client initialized" );
		}
	}

	/// <summary>
	/// Called when the game starts
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		if ( client == Game.LocalClient )
		{
			Log.Info( $"[Game] Local player joined: {client.Name}" );

			// Start authentication flow
			_ = InitializeAsync();
		}
	}

	/// <summary>
	/// Initialize game - authenticate and load state
	/// </summary>
	private async Task InitializeAsync()
	{
		if ( _isInitialized || _isAuthenticating )
		{
			return;
		}

		_isAuthenticating = true;

		try
		{
			Log.Info( "[Game] Starting initialization..." );

			// Step 1: Authenticate with backend
			Log.Info( "[Game] Authenticating..." );
			var authenticated = await _authService.AuthenticateAsync();

			if ( !authenticated )
			{
				Log.Error( "[Game] Authentication failed" );
				// TODO: Show error UI
				return;
			}

			Log.Info( "[Game] Authentication successful" );

			// Step 2: Connect WebSocket for real-time updates
			Log.Info( "[Game] Connecting WebSocket..." );
			var wsConnected = await _websocketService.ConnectAsync();

			if ( !wsConnected )
			{
				Log.Warning( "[Game] WebSocket connection failed, using polling fallback" );
			}
			else
			{
				Log.Info( "[Game] WebSocket connected" );
			}

			// Step 3: Load game state
			Log.Info( "[Game] Loading game state..." );
			var stateLoaded = await _stateManager.LoadStateAsync();

			if ( !stateLoaded )
			{
				Log.Error( "[Game] Failed to load game state" );
				// TODO: Show error UI
				return;
			}

			Log.Info( "[Game] Game state loaded" );

			_isInitialized = true;

			// TODO: Show main game UI
			Log.Info( "[Game] Initialization complete!" );
		}
		catch ( System.Exception ex )
		{
			Log.Error( $"[Game] Initialization failed: {ex.Message}" );
		}
		finally
		{
			_isAuthenticating = false;
		}
	}

	/// <summary>
	/// Called every frame
	/// </summary>
	[Event.Tick]
	public void OnTick()
	{
		if ( !Game.IsClient || !_isInitialized )
		{
			return;
		}

		// Update services (async, fire-and-forget)
		_ = UpdateServicesAsync();
	}

	private async Task UpdateServicesAsync()
	{
		try
		{
			// Update WebSocket (handles polling fallback)
			await _websocketService.UpdateAsync();

			// Update game state (handles auto-sync)
			await _stateManager.UpdateAsync();
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Game] Update error: {ex.Message}" );
		}
	}

	/// <summary>
	/// Called when game is shutting down
	/// </summary>
	public override void Shutdown()
	{
		if ( Game.IsClient )
		{
			_websocketService?.Disconnect();
			Log.Info( "[Game] Client shutdown" );
		}

		base.Shutdown();
	}

	// Console commands for testing

	[ConCmd("game.test_auth")]
	public static async void TestAuthentication()
	{
		if ( Current == null )
		{
			Log.Error( "Game not initialized" );
			return;
		}

		Log.Info( "Testing authentication..." );
		await Current.InitializeAsync();
	}

	[ConCmd("game.refresh")]
	public static async void RefreshState()
	{
		if ( Current == null || Current._stateManager == null )
		{
			Log.Error( "Game not initialized" );
			return;
		}

		Log.Info( "Refreshing game state..." );
		await Current._stateManager.RefreshStateAsync();
	}

	[ConCmd("game.info")]
	public static void ShowInfo()
	{
		if ( Current == null )
		{
			Log.Error( "Game not initialized" );
			return;
		}

		Log.Info( $"Authenticated: {Current._apiClient?.IsAuthenticated ?? false}" );
		Log.Info( $"WebSocket: {Current._websocketService?.IsConnected ?? false}" );
		Log.Info( $"State Loaded: {Current._stateManager?.IsLoaded ?? false}" );

		if ( Current._stateManager?.IsLoaded ?? false )
		{
			var state = Current._stateManager.CurrentState;
			Log.Info( $"Player: {state.PlayerName} ({state.PlayerId})" );
			Log.Info( $"City Position: {state.City.Position}" );

			var resources = Current._stateManager.GetCurrentResources();
			Log.Info( $"Resources: Food={resources.Food}, Wood={resources.Wood}, Stone={resources.Stone}, Metal={resources.Metal}, Gold={resources.Gold}" );
		}
	}

	[ConCmd("game.build")]
	public static async void TestBuild( string buildingType )
	{
		if ( Current == null || Current._actionsService == null )
		{
			Log.Error( "Game not initialized" );
			return;
		}

		Log.Info( $"Testing building upgrade: {buildingType}" );
		var result = await Current._actionsService.StartBuildingUpgradeAsync( buildingType, "inner" );

		if ( result.Success )
		{
			Log.Info( "Building upgrade started successfully!" );
		}
		else
		{
			Log.Error( $"Building upgrade failed: {result.Error}" );
		}
	}

	[ConCmd("game.train")]
	public static async void TestTrain( string troopType, int quantity )
	{
		if ( Current == null || Current._actionsService == null )
		{
			Log.Error( "Game not initialized" );
			return;
		}

		Log.Info( $"Testing troop training: {quantity}x {troopType}" );
		var result = await Current._actionsService.StartTrainingAsync( troopType, quantity );

		if ( result.Success )
		{
			Log.Info( "Training started successfully!" );
		}
		else
		{
			Log.Error( $"Training failed: {result.Error}" );
		}
	}
}
