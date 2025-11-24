namespace Sandbox.Mounting;

/// <summary>
/// Information about a single mount
/// </summary>
public struct MountInfo
{
	/// <summary>
	/// A short, lowercase string that will be used to uniquely identify this asset source
	/// </summary>
	public string Ident { get; init; }

	/// <summary>
	/// The display name of this
	/// </summary>
	public string Title { get; init; }

	/// <summary>
	/// Is this source available, is this game installed? Can we mount it?
	/// </summary>
	public bool Available { get; init; }

	/// <summary>
	/// Is this active and mounted?
	/// </summary>
	public bool Mounted { get; init; }

	public MountInfo( BaseGameMount e )
	{
		Ident = e.Ident;
		Title = e.Title;
		Available = e.IsInstalled;
		Mounted = e.IsMounted;
	}
}
