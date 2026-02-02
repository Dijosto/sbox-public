using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;
using AtlantisStrategy.API;
using AtlantisStrategy.Models;

namespace AtlantisStrategy.Services;

/// <summary>
/// Manages world map data and viewport queries
/// </summary>
public class WorldMapService
{
	private readonly CloudflareClient _client;
	private WorldViewport _currentViewport;
	private Dictionary<string, WorldTile> _tileCache = new();
	private DateTime _lastViewportUpdate = DateTime.MinValue;
	private Vector2Int _lastViewportCenter;
	private int _lastViewportRadius;

	private const float VIEWPORT_UPDATE_INTERVAL = 2.0f; // Update every 2 seconds

	public WorldViewport CurrentViewport => _currentViewport;

	public WorldMapService( CloudflareClient client )
	{
		_client = client;
	}

	/// <summary>
	/// Fetch viewport data from server
	/// </summary>
	public async Task<WorldViewport> GetViewportAsync( Vector2Int center, int radius )
	{
		try
		{
			// Debounce frequent requests
			var timeSinceUpdate = (float)(DateTime.Now - _lastViewportUpdate).TotalSeconds;
			var centerChanged = center.X != _lastViewportCenter.X || center.Y != _lastViewportCenter.Y;
			var radiusChanged = radius != _lastViewportRadius;

			if ( timeSinceUpdate < VIEWPORT_UPDATE_INTERVAL && !centerChanged && !radiusChanged )
			{
				return _currentViewport;
			}

			var minX = center.X - radius;
			var maxX = center.X + radius;
			var minY = center.Y - radius;
			var maxY = center.Y + radius;

			if ( GameConfig.ApiDebug )
			{
				Log.Info( $"[WorldMap] Fetching viewport: ({minX},{minY}) to ({maxX},{maxY})" );
			}

			var viewport = await _client.GetAsync<WorldViewport>(
				$"/api/world/viewport?minX={minX}&maxX={maxX}&minY={minY}&maxY={maxY}"
			);

			if ( viewport != null )
			{
				_currentViewport = viewport;
				_lastViewportUpdate = DateTime.Now;
				_lastViewportCenter = center;
				_lastViewportRadius = radius;

				// Update tile cache
				foreach ( var tile in viewport.Tiles )
				{
					var key = $"{tile.Position.X}_{tile.Position.Y}";
					_tileCache[key] = tile;
				}
			}

			return viewport;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[WorldMap] Failed to fetch viewport: {ex.Message}" );
			return _currentViewport ?? new WorldViewport();
		}
	}

	/// <summary>
	/// Get details for a specific tile
	/// </summary>
	public async Task<WorldTile> GetTileAsync( Vector2Int position )
	{
		try
		{
			// Check cache first
			var key = $"{position.X}_{position.Y}";
			if ( _tileCache.ContainsKey( key ) )
			{
				return _tileCache[key];
			}

			// Fetch from server
			var tile = await _client.GetAsync<WorldTile>(
				$"/api/world/tile?x={position.X}&y={position.Y}"
			);

			if ( tile != null )
			{
				_tileCache[key] = tile;
			}

			return tile;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[WorldMap] Failed to fetch tile: {ex.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Search for tiles (e.g., nearest wilderness, player cities)
	/// </summary>
	public async Task<WorldSearchResult> SearchAsync( string query, int limit = 20 )
	{
		try
		{
			var encoded = System.Uri.EscapeDataString( query );
			var result = await _client.GetAsync<WorldSearchResult>(
				$"/api/world/search?q={encoded}&limit={limit}"
			);

			return result ?? new WorldSearchResult();
		}
		catch ( Exception ex )
		{
			Log.Error( $"[WorldMap] Search failed: {ex.Message}" );
			return new WorldSearchResult();
		}
	}

	/// <summary>
	/// Find nearest unclaimed wilderness
	/// </summary>
	public async Task<List<WorldTile>> FindNearestWildernessAsync( Vector2Int center, string resourceType = null )
	{
		try
		{
			var url = $"/api/world/nearest/wilderness?x={center.X}&y={center.Y}";
			if ( !string.IsNullOrEmpty( resourceType ) )
			{
				url += $"&resource={resourceType}";
			}

			var result = await _client.GetAsync<WorldSearchResult>( url );
			return result?.Results ?? new List<WorldTile>();
		}
		catch ( Exception ex )
		{
			Log.Error( $"[WorldMap] Failed to find wilderness: {ex.Message}" );
			return new List<WorldTile>();
		}
	}

	/// <summary>
	/// Find nearest NPC camps
	/// </summary>
	public async Task<List<WorldTile>> FindNearestNPCCampsAsync( Vector2Int center, int maxLevel = 0 )
	{
		try
		{
			var url = $"/api/world/nearest/npc?x={center.X}&y={center.Y}";
			if ( maxLevel > 0 )
			{
				url += $"&maxLevel={maxLevel}";
			}

			var result = await _client.GetAsync<WorldSearchResult>( url );
			return result?.Results ?? new List<WorldTile>();
		}
		catch ( Exception ex )
		{
			Log.Error( $"[WorldMap] Failed to find NPC camps: {ex.Message}" );
			return new List<WorldTile>();
		}
	}

	/// <summary>
	/// Search for player by name
	/// </summary>
	public async Task<List<WorldTile>> SearchPlayerAsync( string playerName )
	{
		try
		{
			var encoded = System.Uri.EscapeDataString( playerName );
			var result = await _client.GetAsync<WorldSearchResult>(
				$"/api/world/search/player?name={encoded}"
			);

			return result?.Results ?? new List<WorldTile>();
		}
		catch ( Exception ex )
		{
			Log.Error( $"[WorldMap] Failed to search player: {ex.Message}" );
			return new List<WorldTile>();
		}
	}

	/// <summary>
	/// Clear tile cache (call when significant world changes occur)
	/// </summary>
	public void ClearCache()
	{
		_tileCache.Clear();
		_lastViewportUpdate = DateTime.MinValue;
	}
}
