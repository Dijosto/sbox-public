using Sandbox.Modals;
using System.Text.Json.Nodes;

namespace Sandbox;

public static partial class Storage
{
	/// <summary>
	/// A folder of content stored on disk
	/// </summary>
	public sealed class Entry
	{
		/// <summary>
		/// The identity of this content
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// The type of content, eg "save", "dupe"
		/// </summary>
		public string Type { get; private set; }

		/// <summary>
		/// Metadata about this content. This gets saved to disk automatically.
		/// </summary>
		private Dictionary<string, string> Meta { get; } = new();

		/// <summary>
		/// When this content was created
		/// </summary>
		public DateTimeOffset Created { get; private set; } = DateTimeOffset.UtcNow;

		/// <summary>
		/// This is where you save and load your files to
		/// </summary>
		public BaseFileSystem Files { get; private set; }

		private string DataPath => $"/storage/{Type}/{Id}/";
		private string MetaPath => "_meta.json";
		private string ThumbPath => "_thumb.png";

		public Entry( string type )
		{
			Type = type;
			Id = Guid.NewGuid().ToString();

			ValidateType();

			Sandbox.FileSystem.Data.CreateDirectory( DataPath );
			Files = Sandbox.FileSystem.Data.CreateSubSystem( DataPath );

			SaveMeta();
		}

		internal Entry( StorageMeta meta )
		{
			Id = meta.Id;
			Type = meta.Type;
			Meta = meta.Meta?.ToDictionary() ?? this.Meta;
			Created = meta.Timestamp;

			ValidateType();

			Sandbox.FileSystem.Data.CreateDirectory( DataPath );
			Files = Sandbox.FileSystem.Data.CreateSubSystem( DataPath );
		}

		internal Entry( BaseFileSystem fs )
		{
			var meta = fs.ReadJsonOrDefault<StorageMeta>( MetaPath );
			if ( meta == null ) throw new System.Exception( "meta file not found" );

			Id = meta.Id;
			Type = meta.Type;
			Meta = meta.Meta?.ToDictionary() ?? this.Meta;
			Created = meta.Timestamp;

			ValidateType();

			Files = fs;
		}

		private void ValidateType()
		{
			ArgumentNullException.ThrowIfNull( Type );

			if ( Type.Length < 1 ) throw new System.ArgumentException( "type cannot be empty", nameof( Type ) );
			if ( Type.Length > 16 ) throw new System.ArgumentException( "type should be under 16 characters", nameof( Type ) );

			// TODO - validate type and filename

			// type should be letters only, no symbols
			if ( !System.Text.RegularExpressions.Regex.IsMatch( Type, "^[a-zA-Z]+$" ) )
			{
				throw new System.ArgumentException( "Invalid storage type", nameof( Type ) );
			}
		}

		/// <summary>
		/// Set a meta value
		/// </summary>
		public void SetMeta<T>( string key, T value )
		{
			if ( Files.IsReadOnly ) return;

			if ( value == null )
			{
				Meta.Remove( key );
				return;
			}

			Meta[key] = JsonValue.Create( value )?.ToJsonString();
			SaveMeta();
		}

		/// <summary>
		/// Get a meta value
		/// </summary>
		public T GetMeta<T>( string key, T defaultValue = default )
		{
			if ( Meta.TryGetValue( key, out var val ) )
			{
				return Json.Deserialize<T>( val );
			}

			return defaultValue;
		}

		void SaveMeta()
		{
			var meta = new StorageMeta
			{
				Id = this.Id,
				Type = this.Type,
				Meta = this.Meta,
				Timestamp = this.Created
			};

			Files.WriteJson( MetaPath, meta );
		}

		bool _thumbGenerated = false;
		Texture _thumbnail;

		public Texture Thumbnail
		{
			get
			{
				if ( _thumbGenerated ) return _thumbnail;
				_thumbGenerated = true;
				if ( !Files.FileExists( ThumbPath ) ) return null;

				var data = Files.ReadAllBytes( ThumbPath );
				using var bitmap = Bitmap.CreateFromBytes( data.ToArray() );
				_thumbnail = bitmap?.ToTexture();

				return _thumbnail;
			}
		}


		public void SetThumbnail( Bitmap bitmap )
		{
			if ( Files.IsReadOnly ) return;

			_thumbGenerated = false;

			var png = bitmap.ToPng();
			Files.WriteAllBytes( ThumbPath, png );
		}

		public void Delete()
		{
			Sandbox.FileSystem.Data.DeleteDirectory( DataPath, true );
			Storage.OnDeleted( this );
		}

		public void Publish( string title = "Unnammed", string[] tags = null, Dictionary<string, string> keyvalues = null )
		{
			var meta = Files.ReadAllText( MetaPath );

			var o = new WorkshopPublishOptions
			{
				Title = title,
				StorageEntry = this,
				KeyValues = [],
				Tags = [Type],
				Metadata = meta
			};

			if ( tags != null )
			{
				foreach ( var t in tags )
				{
					o.Tags.Add( t );
				}
			}

			if ( keyvalues != null )
			{
				foreach ( var t in keyvalues )
				{
					o.KeyValues[t.Key] = t.Value;
				}
			}

			// assign the thumbnail
			if ( Files.FileExists( ThumbPath ) )
			{
				var thumbData = Files.ReadAllBytes( ThumbPath );
				var bitmap = Bitmap.CreateFromBytes( thumbData.ToArray() );
				o.Thumbnail = bitmap;
			}

			o.KeyValues["type"] = Type;
			o.KeyValues["source"] = "storage";

			Game.Overlay.WorkshopPublish( o );
		}
	}
}
