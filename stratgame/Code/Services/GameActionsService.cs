using System;
using System.Threading.Tasks;
using Sandbox;
using AtlantisStrategy.API;
using AtlantisStrategy.Models;

namespace AtlantisStrategy.Services;

/// <summary>
/// Service for sending player actions to the server.
/// All methods return results from server validation.
/// Client never predicts outcomes - always waits for server response.
/// </summary>
public class GameActionsService
{
	private readonly CloudflareClient _client;
	private readonly GameStateManager _stateManager;

	public GameActionsService( CloudflareClient client, GameStateManager stateManager )
	{
		_client = client;
		_stateManager = stateManager;
	}

	#region Building Actions

	/// <summary>
	/// Start building upgrade
	/// </summary>
	public async Task<ActionResult> StartBuildingUpgradeAsync( string buildingType, string zone )
	{
		try
		{
			var request = new
			{
				buildingType,
				zone,
				timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			var result = await _client.PostAsync<ActionResult>( "/api/building/upgrade", request );

			if ( result.Success )
			{
				Log.Info( $"[Actions] Building {buildingType} upgrade started" );

				// Refresh state to get updated queue
				_ = _stateManager.RefreshStateAsync();
			}
			else
			{
				Log.Warning( $"[Actions] Building upgrade failed: {result.Error}" );
			}

			return result;
		}
		catch ( ApiException ex )
		{
			return new ActionResult
			{
				Success = false,
				Error = ex.Message
			};
		}
	}

	/// <summary>
	/// Cancel building upgrade
	/// </summary>
	public async Task<ActionResult> CancelBuildingAsync( string queueId )
	{
		try
		{
			var result = await _client.DeleteAsync<ActionResult>( $"/api/building/queue/{queueId}" );

			if ( result.Success )
			{
				Log.Info( $"[Actions] Building cancelled: {queueId}" );
				_ = _stateManager.RefreshStateAsync();
			}

			return result;
		}
		catch ( ApiException ex )
		{
			return new ActionResult { Success = false, Error = ex.Message };
		}
	}

	#endregion

	#region Training Actions

	/// <summary>
	/// Start training troops
	/// </summary>
	public async Task<ActionResult> StartTrainingAsync( string troopType, int quantity )
	{
		try
		{
			var request = new
			{
				troopType,
				quantity,
				timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			var result = await _client.PostAsync<ActionResult>( "/api/troops/train", request );

			if ( result.Success )
			{
				Log.Info( $"[Actions] Training {quantity}x {troopType} started" );
				_ = _stateManager.RefreshStateAsync();
			}
			else
			{
				Log.Warning( $"[Actions] Training failed: {result.Error}" );
			}

			return result;
		}
		catch ( ApiException ex )
		{
			return new ActionResult { Success = false, Error = ex.Message };
		}
	}

	/// <summary>
	/// Cancel training queue item
	/// </summary>
	public async Task<ActionResult> CancelTrainingAsync( string queueId )
	{
		try
		{
			var result = await _client.DeleteAsync<ActionResult>( $"/api/troops/queue/{queueId}" );

			if ( result.Success )
			{
				Log.Info( $"[Actions] Training cancelled: {queueId}" );
				_ = _stateManager.RefreshStateAsync();
			}

			return result;
		}
		catch ( ApiException ex )
		{
			return new ActionResult { Success = false, Error = ex.Message };
		}
	}

	#endregion

	#region Research Actions

	/// <summary>
	/// Start research
	/// </summary>
	public async Task<ActionResult> StartResearchAsync( string researchType )
	{
		try
		{
			var request = new
			{
				researchType,
				timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			var result = await _client.PostAsync<ActionResult>( "/api/research/start", request );

			if ( result.Success )
			{
				Log.Info( $"[Actions] Research {researchType} started" );
				_ = _stateManager.RefreshStateAsync();
			}
			else
			{
				Log.Warning( $"[Actions] Research failed: {result.Error}" );
			}

			return result;
		}
		catch ( ApiException ex )
		{
			return new ActionResult { Success = false, Error = ex.Message };
		}
	}

	#endregion

	#region March Actions

	/// <summary>
	/// Send troops to attack a target
	/// </summary>
	public async Task<ActionResult<MarchResult>> AttackAsync( Vector2Int target, TroopSelection troops )
	{
		try
		{
			var request = new
			{
				target,
				troops,
				marchType = "attack",
				timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			var result = await _client.PostAsync<ActionResult<MarchResult>>( "/api/march/start", request );

			if ( result.Success )
			{
				Log.Info( $"[Actions] Attack march sent to {target}" );
				_ = _stateManager.RefreshStateAsync();
			}
			else
			{
				Log.Warning( $"[Actions] March failed: {result.Error}" );
			}

			return result;
		}
		catch ( ApiException ex )
		{
			return new ActionResult<MarchResult> { Success = false, Error = ex.Message };
		}
	}

	/// <summary>
	/// Send troops to reinforce a target
	/// </summary>
	public async Task<ActionResult<MarchResult>> ReinforceAsync( Vector2Int target, TroopSelection troops )
	{
		try
		{
			var request = new
			{
				target,
				troops,
				marchType = "reinforce",
				timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			var result = await _client.PostAsync<ActionResult<MarchResult>>( "/api/march/start", request );

			if ( result.Success )
			{
				Log.Info( $"[Actions] Reinforcement sent to {target}" );
				_ = _stateManager.RefreshStateAsync();
			}

			return result;
		}
		catch ( ApiException ex )
		{
			return new ActionResult<MarchResult> { Success = false, Error = ex.Message };
		}
	}

	/// <summary>
	/// Recall a march
	/// </summary>
	public async Task<ActionResult> RecallMarchAsync( string marchId )
	{
		try
		{
			var result = await _client.PostAsync<ActionResult>( $"/api/march/{marchId}/recall", null );

			if ( result.Success )
			{
				Log.Info( $"[Actions] March recalled: {marchId}" );
				_ = _stateManager.RefreshStateAsync();
			}

			return result;
		}
		catch ( ApiException ex )
		{
			return new ActionResult { Success = false, Error = ex.Message };
		}
	}

	#endregion
}

/// <summary>
/// Generic action result from server
/// </summary>
public class ActionResult
{
	public bool Success { get; set; }
	public string Error { get; set; }
}

/// <summary>
/// Generic action result with data
/// </summary>
public class ActionResult<T>
{
	public bool Success { get; set; }
	public string Error { get; set; }
	public T Data { get; set; }
}

/// <summary>
/// March creation result
/// </summary>
public class MarchResult
{
	public string MarchId { get; set; }
	public long ArrivalTime { get; set; }
	public int TravelDurationSeconds { get; set; }
}

/// <summary>
/// Troop selection for march
/// </summary>
public class TroopSelection
{
	public int Infantry { get; set; }
	public int Archers { get; set; }
	public int Cavalry { get; set; }
	public int Transport { get; set; }
}
