namespace Sandbox;

/// <summary>
/// Some metadata we'll pack into a workshop submission when publishing.
/// </summary>
public struct WorkshopItemMetaData
{
	public string Title { get; set; }
	public string PackageIdent { get; set; }
	public ulong WorkshopId { get; set; }
}
