using System.Text.Json.Serialization;
using System.Threading;

namespace Sandbox;

public static partial class Storage
{
	/// <summary>
	/// Install a workshop file, return a LocalFileSystem to it
	/// </summary>
	internal static async Task<LocalFileSystem> InstallWorkshopFile( ulong published_file_id, CancellationToken token = default )
	{
		using var file = NativeEngine.CUgcInstall.Create( published_file_id );

		while ( !file.m_complete )
		{
			await Task.Delay( 10, token );
		}

		token.ThrowIfCancellationRequested();

		if ( !file.m_success ) return null;

		var json = Json.Deserialize<InstalledJson>( file.GetResultJson() );
		if ( json == null ) return null;
		if ( json.InstallFolder == null ) return null;

		return new LocalFileSystem( json.InstallFolder, true );
	}

	/// <summary>
	/// The data returned by CUgcInstall
	/// </summary>
	class InstalledJson
	{
		public ulong PublishedFileId { get; set; }
		public string InstallFolder { get; set; }
		public ulong SizeOnDisk { get; set; }
		[JsonConverter( typeof( UnixTimestampConverter ) )]
		public DateTimeOffset Timestamp { get; set; }
		public bool Subscribed { get; set; }
	}

}
