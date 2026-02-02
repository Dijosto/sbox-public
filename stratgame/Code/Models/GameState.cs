using System.Collections.Generic;

namespace AtlantisStrategy.Models;

/// <summary>
/// Complete player game state from server
/// This is the "source of truth" - client never modifies this directly
/// </summary>
public class PlayerState
{
	public string PlayerId { get; set; }
	public string PlayerName { get; set; }
	public CityState City { get; set; }
	public Resources Resources { get; set; }
	public Dictionary<string, int> Research { get; set; } = new();
	public List<TroopStack> Troops { get; set; } = new();
	public List<March> ActiveMarches { get; set; } = new();
	public DragonState Dragon { get; set; }
	public List<OutpostState> Outposts { get; set; } = new();
	public AllianceInfo Alliance { get; set; }
	public long LastUpdateTimestamp { get; set; }
}

/// <summary>
/// City state with buildings and queues
/// </summary>
public class CityState
{
	public Vector2Int Position { get; set; }
	public Dictionary<string, BuildingState> InnerCity { get; set; } = new();
	public Dictionary<string, BuildingState> OuterFields { get; set; } = new();
	public List<BuildQueueItem> BuildQueue { get; set; } = new();
	public List<TrainQueueItem> TrainQueue { get; set; } = new();
	public int MaxWorkers { get; set; } = 1;
	public int UsedWorkers { get; set; } = 0;
}

/// <summary>
/// Individual building state
/// </summary>
public class BuildingState
{
	public string BuildingType { get; set; }
	public int Level { get; set; }
	public long CompletionTime { get; set; } = 0; // 0 if not upgrading
}

/// <summary>
/// Item in the construction queue
/// </summary>
public class BuildQueueItem
{
	public string QueueId { get; set; }
	public string BuildingType { get; set; }
	public string Zone { get; set; } // "inner" or "outer"
	public int ToLevel { get; set; }
	public long StartTime { get; set; }
	public long CompletionTime { get; set; }
}

/// <summary>
/// Item in the training queue
/// </summary>
public class TrainQueueItem
{
	public string QueueId { get; set; }
	public string TroopType { get; set; }
	public int Quantity { get; set; }
	public long StartTime { get; set; }
	public long CompletionTime { get; set; }
}

/// <summary>
/// Resource amounts
/// </summary>
public class Resources
{
	public int Food { get; set; }
	public int Wood { get; set; }
	public int Stone { get; set; }
	public int Metal { get; set; }
	public int Gold { get; set; }
	public int PremiumCurrency { get; set; }

	// Production rates (per hour)
	public float FoodRate { get; set; }
	public float WoodRate { get; set; }
	public float StoneRate { get; set; }
	public float MetalRate { get; set; }
	public float GoldRate { get; set; }

	// Capacity limits
	public int FoodCap { get; set; }
	public int WoodCap { get; set; }
	public int StoneCap { get; set; }
	public int MetalCap { get; set; }
	public int GoldCap { get; set; }
}

/// <summary>
/// Stack of troops (same type)
/// </summary>
public class TroopStack
{
	public string TroopType { get; set; }
	public int Quantity { get; set; }
	public string Location { get; set; } // "city", "march:{id}", "outpost:{id}"
}

/// <summary>
/// Active march (army moving on map)
/// </summary>
public class March
{
	public string MarchId { get; set; }
	public Vector2Int Origin { get; set; }
	public Vector2Int Destination { get; set; }
	public List<TroopStack> Army { get; set; }
	public long StartTime { get; set; }
	public long ArrivalTime { get; set; }
	public string MarchType { get; set; } // "attack", "reinforce", "transport", "return"
}

/// <summary>
/// Dragon state
/// </summary>
public class DragonState
{
	public string DragonType { get; set; } // "great", "fire", "water", etc.
	public int Level { get; set; }
	public int Armor { get; set; }
	public bool HasEgg { get; set; }
	public bool HasOutpost { get; set; }
}

/// <summary>
/// Outpost (expansion city)
/// </summary>
public class OutpostState
{
	public string OutpostId { get; set; }
	public string DragonType { get; set; }
	public Vector2Int Position { get; set; }
	public Dictionary<string, BuildingState> Buildings { get; set; } = new();
	public List<TroopStack> Troops { get; set; } = new();
}

/// <summary>
/// Alliance information
/// </summary>
public class AllianceInfo
{
	public string AllianceId { get; set; }
	public string AllianceName { get; set; }
	public string AllianceTag { get; set; }
	public string Role { get; set; } // "member", "officer", "leader"
	public int MemberCount { get; set; }
}

/// <summary>
/// 2D integer vector for map positions
/// </summary>
public struct Vector2Int
{
	public int X { get; set; }
	public int Y { get; set; }

	public Vector2Int( int x, int y )
	{
		X = x;
		Y = y;
	}

	public override string ToString() => $"({X}, {Y})";
}
