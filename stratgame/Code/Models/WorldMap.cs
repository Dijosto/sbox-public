using System.Collections.Generic;

namespace AtlantisStrategy.Models;

/// <summary>
/// World map viewport data from server
/// </summary>
public class WorldViewport
{
	public Vector2Int ViewportMin { get; set; }
	public Vector2Int ViewportMax { get; set; }
	public List<WorldTile> Tiles { get; set; } = new();
	public List<MarchVisual> Marches { get; set; } = new();
}

/// <summary>
/// Single tile on the world map
/// </summary>
public class WorldTile
{
	public Vector2Int Position { get; set; }
	public string TileType { get; set; } // "city", "npc_camp", "wilderness", "outpost", "empty"
	public string OwnerId { get; set; } // Player ID if owned
	public string OwnerName { get; set; }
	public int Level { get; set; }
	public string AllianceTag { get; set; }

	// For wilderness tiles
	public string ResourceType { get; set; } // "food", "wood", "stone", "metal"
	public int ResourceBonus { get; set; }

	// For NPC camps
	public int NpcLevel { get; set; }
	public int NpcTroopCount { get; set; }
}

/// <summary>
/// Visual representation of a march on the map
/// </summary>
public class MarchVisual
{
	public string MarchId { get; set; }
	public Vector2Int Origin { get; set; }
	public Vector2Int Destination { get; set; }
	public float Progress { get; set; } // 0.0 to 1.0
	public string MarchType { get; set; } // "attack", "reinforce", "transport", "return"
	public string OwnerName { get; set; }
	public bool IsHostile { get; set; } // For incoming attacks
}

/// <summary>
/// Search result for map queries
/// </summary>
public class WorldSearchResult
{
	public List<WorldTile> Results { get; set; } = new();
	public int TotalResults { get; set; }
}
