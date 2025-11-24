using System.IO;

namespace Editor;

public record RecentsLocation : LocalAssetBrowser.Location
{
	public override bool IsAggregate => true;

	public RecentsLocation() : base( "Recents", "History" )
	{
		Path = "@recents";
	}

	public override bool CanGoUp() => false;
	public override IEnumerable<LocalAssetBrowser.Location> GetDirectories() => Enumerable.Empty<LocalAssetBrowser.Location>();

	public override IEnumerable<FileInfo> GetFiles()
	{
		foreach ( var asset in AssetSystem.All.OrderByDescending( x => x.LastOpened ).Take( 50 ) )
		{
			yield return new FileInfo( asset.AbsolutePath );
		}
	}
}
