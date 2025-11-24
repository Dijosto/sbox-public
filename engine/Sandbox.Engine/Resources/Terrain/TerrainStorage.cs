using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sandbox;

/// <summary>
/// Stores heightmaps, control maps and materials.
/// </summary>
[Expose]
[AssetType( Name = "Terrain", Extension = "terrain", Category = "World", Flags = AssetTypeFlags.NoEmbedding )]
public partial class TerrainStorage : GameResource
{
	[JsonInclude, JsonPropertyName( "Maps" )] private TerrainMaps Maps { get; set; } = new();

	[JsonIgnore] public ushort[] HeightMap { get => Maps.HeightMap; set => Maps.HeightMap = value; }
	[JsonIgnore] public Color32[] ControlMap { get => Maps.SplatMap; set => Maps.SplatMap = value; }
	[JsonIgnore] public byte[] HolesMap { get => Maps.HolesMap; set => Maps.HolesMap = value; }

	public int Resolution { get; set; }

	/// <summary>
	/// Uniform world size of the width and length of the terrain.
	/// </summary>
	public float TerrainSize { get; set; }

	/// <summary>
	/// World size of the maximum height of the terrain.
	/// </summary>
	public float TerrainHeight { get; set; }

	public List<TerrainMaterial> Materials { get; set; } = new();

	public class TerrainMaterialSettings
	{
		[Group( "Height Blend" ), Property] public bool HeightBlendEnabled { get; set; } = true;
		[Group( "Height Blend" ), Property, Range( 0, 1 )] public float HeightBlendSharpness { get; set; } = 0.87f;
	}

	public TerrainMaterialSettings MaterialSettings { get; set; } = new();

	public TerrainStorage()
	{
		SetResolution( 512 );
		TerrainSize = 20000;
		TerrainHeight = 10000;
	}

	public void SetResolution( int resolution )
	{
		Resolution = resolution;

		HeightMap = new ushort[Resolution * Resolution];
		ControlMap = new Color32[Resolution * Resolution];
		HolesMap = new byte[Resolution * Resolution];

		for ( int i = 0; i < ControlMap.Length; i++ )
			ControlMap[i] = new Color32( 255, 0, 0, 0 );
	}

	internal void GetDominantControlMapIndices( byte[] indices )
	{
		Assert.True( indices.Length == ControlMap.Length );
		// Assert.True( indices.Length == HolesMap.Length );

		// Resolve the getters
		var holeMap = HolesMap;
		var controlMap = ControlMap;

		for ( var i = 0; i < controlMap.Length; i++ )
		{
			if ( holeMap is not null && i < holeMap.Length && holeMap[i] != 0 )
			{
				indices[i] = 255;
				continue;
			}

			var color = controlMap[i];
			var max = color.r;
			byte maxIndex = 0;

			if ( color.g > max )
			{
				max = color.g;
				maxIndex = 1;
			}

			if ( color.b > max )
			{
				max = color.b;
				maxIndex = 2;
			}

			if ( color.a > max )
			{
				maxIndex = 3;
			}

			indices[i] = maxIndex;
		}
	}

	public byte[] GetDominantControlMapIndices( int x, int y, int w, int h )
	{
		var indices = new byte[w * h];

		for ( int offsetY = 0; offsetY < h; offsetY++ )
		{
			for ( int offsetX = 0; offsetX < w; offsetX++ )
			{
				int sourceIndex = (x + offsetX) + (y + offsetY) * Resolution;
				int destinationIndex = offsetX + offsetY * w;

				if ( HolesMap != null && HolesMap[sourceIndex] != 0 )
				{
					indices[destinationIndex] = 255;
					continue;
				}

				var color = ControlMap[sourceIndex];
				var max = color.r;
				byte maxIndex = 0;

				if ( color.g > max )
				{
					max = color.g;
					maxIndex = 1;
				}

				if ( color.b > max )
				{
					max = color.b;
					maxIndex = 2;
				}

				if ( color.a > max )
				{
					maxIndex = 3;
				}

				indices[destinationIndex] = maxIndex;
			}
		}

		return indices;
	}

	/// <summary>
	/// Contains terrain maps that get compressed
	/// </summary>
	private class TerrainMaps : IJsonConvert
	{
		public ushort[] HeightMap { get; set; }
		public Color32[] SplatMap { get; set; }
		public byte[] HolesMap { get; set; }

		public static object JsonRead( ref Utf8JsonReader reader, Type typeToConvert )
		{
			if ( reader.TokenType != JsonTokenType.StartObject )
				return null;

			var maps = new TerrainMaps();

			reader.Read();

			while ( reader.TokenType != JsonTokenType.EndObject )
			{
				if ( reader.TokenType == JsonTokenType.PropertyName )
				{
					var name = reader.GetString();
					reader.Read();

					if ( name == "heightmap" )
					{
						maps.HeightMap = Decompress<ushort>( reader.GetBytesFromBase64() ).ToArray();
						reader.Read();
						continue;
					}

					if ( name == "splatmap" )
					{
						maps.SplatMap = Decompress<Color32>( reader.GetBytesFromBase64() ).ToArray();
						reader.Read();
						continue;
					}

					if ( name == "holesmap" )
					{
						maps.HolesMap = Decompress<byte>( reader.GetBytesFromBase64() ).ToArray();
						reader.Read();
						continue;
					}

					reader.Read();
					continue;

				}

				reader.Read();
			}


			return maps;
		}

		public static void JsonWrite( object value, Utf8JsonWriter writer )
		{
			if ( value is not TerrainMaps maps )
				throw new NotImplementedException();

			writer.WriteStartObject();
			writer.WriteBase64String( "heightmap", Compress( maps.HeightMap.AsSpan() ) );
			writer.WriteBase64String( "splatmap", Compress( maps.SplatMap.AsSpan() ) );
			writer.WriteBase64String( "holesmap", Compress( maps.HolesMap.AsSpan() ) );
			writer.WriteEndObject();
		}

		internal static Span<T> Decompress<T>( byte[] compressedData ) where T : unmanaged
		{
			using var compressedStream = new MemoryStream( compressedData );
			using var decompressedStream = new MemoryStream();
			using ( var deflateStream = new DeflateStream( compressedStream, CompressionMode.Decompress ) )
			{
				deflateStream.CopyTo( decompressedStream );
			}

			return MemoryMarshal.Cast<byte, T>( decompressedStream.ToArray().AsSpan() );
		}

		internal static byte[] Compress<T>( Span<T> data ) where T : struct
		{
			// Deflate compress the data
			using var memoryStream = new MemoryStream();
			using ( var deflateStream = new DeflateStream( memoryStream, CompressionMode.Compress ) )
			{
				deflateStream.Write( MemoryMarshal.AsBytes<T>( data ) );
			}

			return memoryStream.ToArray();
		}


	}

	protected override Bitmap CreateAssetTypeIcon( int width, int height )
	{
		return CreateSimpleAssetTypeIcon( "landscape", width, height );
	}
}
