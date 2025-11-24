namespace Sandbox;

/// <summary>
/// Configurable options when spawning a networked object.
/// </summary>
public struct NetworkSpawnOptions()
{
	/// <summary>
	/// The default network spawn options.
	/// </summary>
	public static readonly NetworkSpawnOptions Default = new();

	/// <summary>
	/// What happens to this networked object when its owner disconnects?
	/// </summary>
	public NetworkOrphaned? OrphanedMode { get; set; }

	/// <summary>
	/// Who can control the ownership of this networked object?
	/// </summary>
	public OwnerTransfer? OwnerTransfer { get; set; }

	/// <summary>
	/// Determines whether updates for this networked object are always transmitted to clients. Otherwise,
	/// they are only transmitted when the object is determined as visible to each client.
	/// </summary>
	public bool? AlwaysTransmit { get; set; }

	/// <summary>
	/// Should this networked object start enabled?
	/// </summary>
	public bool StartEnabled { get; set; } = true;

	/// <summary>
	/// Who should be the owner of this networked object?
	/// </summary>
	public Connection Owner { get; set; }
}
