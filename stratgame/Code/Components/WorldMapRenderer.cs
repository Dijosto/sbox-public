using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;
using AtlantisStrategy.Models;
using AtlantisStrategy.Services;

namespace AtlantisStrategy.Components;

/// <summary>
/// Renders the world map in 3D
/// Handles viewport updates and tile visualization
/// </summary>
public sealed class WorldMapRenderer : Component
{
	[Property] public float TileSize { get; set; } = 100f;
	[Property] public int ViewportRadius { get; set; } = 25;
	[Property] public Color EmptyTileColor { get; set; } = Color.FromRgb( 0x1a1a1a );
	[Property] public Color CityTileColor { get; set; } = Color.FromRgb( 0x4a90e2 );
	[Property] public Color NPCTileColor { get; set; } = Color.FromRgb( 0xe24a4a );
	[Property] public Color WildernessTileColor { get; set; } = Color.FromRgb( 0x4ae24a );

	private WorldMapService _worldMapService;
	private Dictionary<string, GameObject> _tileObjects = new();
	private Vector2Int _currentCenter;
	private DateTime _lastUpdate = DateTime.MinValue;
	private const float UPDATE_INTERVAL = 1.0f;

	protected override void OnStart()
	{
		if ( !Game.IsClient )
		{
			return;
		}

		if ( Game.Current == null )
		{
			Log.Error( "[WorldMapRenderer] Game.Current is null" );
			return;
		}

		// Get world map service from game
		_worldMapService = new WorldMapService( Game.Current.ApiClient );

		Log.Info( "[WorldMapRenderer] Initialized" );
	}

	protected override void OnUpdate()
	{
		if ( !Game.IsClient || _worldMapService == null )
		{
			return;
		}

		// Update viewport periodically
		var timeSinceUpdate = (float)(DateTime.Now - _lastUpdate).TotalSeconds;
		if ( timeSinceUpdate >= UPDATE_INTERVAL )
		{
			_ = UpdateViewportAsync();
			_lastUpdate = DateTime.Now;
		}
	}

	private async Task UpdateViewportAsync()
	{
		try
		{
			// Get player's city position as center
			var state = Game.Current?.StateManager?.CurrentState;
			if ( state == null || state.City == null )
			{
				return;
			}

			var center = state.City.Position;

			// Fetch viewport from server
			var viewport = await _worldMapService.GetViewportAsync( center, ViewportRadius );

			if ( viewport == null || viewport.Tiles == null )
			{
				return;
			}

			// Update center
			_currentCenter = center;

			// Render tiles
			RenderTiles( viewport.Tiles );

			// Render marches
			RenderMarches( viewport.Marches );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[WorldMapRenderer] Update failed: {ex.Message}" );
		}
	}

	private void RenderTiles( List<WorldTile> tiles )
	{
		var seenKeys = new HashSet<string>();

		foreach ( var tile in tiles )
		{
			var key = $"{tile.Position.X}_{tile.Position.Y}";
			seenKeys.Add( key );

			// Get or create tile object
			if ( !_tileObjects.TryGetValue( key, out var tileObject ) )
			{
				tileObject = CreateTileObject( tile );
				_tileObjects[key] = tileObject;
			}
			else
			{
				// Update existing tile
				UpdateTileObject( tileObject, tile );
			}
		}

		// Remove tiles outside viewport
		var toRemove = new List<string>();
		foreach ( var kvp in _tileObjects )
		{
			if ( !seenKeys.Contains( kvp.Key ) )
			{
				kvp.Value.Destroy();
				toRemove.Add( kvp.Key );
			}
		}

		foreach ( var key in toRemove )
		{
			_tileObjects.Remove( key );
		}
	}

	private GameObject CreateTileObject( WorldTile tile )
	{
		var obj = Scene.CreateObject();
		obj.Name = $"Tile_{tile.Position.X}_{tile.Position.Y}";
		obj.WorldPosition = new Vector3( tile.Position.X * TileSize, tile.Position.Y * TileSize, 0 );

		// Add model renderer (simple cube for now)
		var modelRenderer = obj.AddComponent<ModelRenderer>();
		modelRenderer.Model = Model.Load( "models/dev/box.vmdl" );

		// Scale to tile size
		obj.WorldScale = new Vector3( TileSize * 0.9f, TileSize * 0.9f, 10f );

		// Set color based on type
		UpdateTileObject( obj, tile );

		return obj;
	}

	private void UpdateTileObject( GameObject obj, WorldTile tile )
	{
		var modelRenderer = obj.Components.Get<ModelRenderer>();
		if ( modelRenderer == null )
		{
			return;
		}

		// Color based on tile type
		var color = tile.TileType switch
		{
			"city" => CityTileColor,
			"npc_camp" => NPCTileColor,
			"wilderness" => WildernessTileColor,
			"outpost" => CityTileColor * 0.8f,
			_ => EmptyTileColor
		};

		modelRenderer.Tint = color;

		// Add height variation for non-empty tiles
		if ( tile.TileType != "empty" )
		{
			var scale = obj.WorldScale;
			scale.z = 10f + (tile.Level * 5f);
			obj.WorldScale = scale;
		}
	}

	private void RenderMarches( List<MarchVisual> marches )
	{
		// TODO: Render march arrows/lines between origin and destination
		// For now, just log them
		foreach ( var march in marches )
		{
			if ( GameConfig.ApiDebug && march.IsHostile )
			{
				Log.Info( $"[WorldMapRenderer] Hostile march {march.MarchId} at {march.Progress * 100:F0}%" );
			}
		}
	}

	/// <summary>
	/// Get world position from tile coordinates
	/// </summary>
	public Vector3 TileToWorld( Vector2Int tile )
	{
		return new Vector3( tile.X * TileSize, tile.Y * TileSize, 0 );
	}

	/// <summary>
	/// Get tile coordinates from world position
	/// </summary>
	public Vector2Int WorldToTile( Vector3 worldPos )
	{
		return new Vector2Int(
			(int)Math.Floor( worldPos.x / TileSize ),
			(int)Math.Floor( worldPos.y / TileSize )
		);
	}

	protected override void OnDestroy()
	{
		// Clean up all tile objects
		foreach ( var obj in _tileObjects.Values )
		{
			obj?.Destroy();
		}

		_tileObjects.Clear();
	}
}
