using SkiaSharp;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sandbox;

public partial class TerrainStorage
{
	public override int ResourceVersion => 1;

	[Expose, JsonUpgrader( typeof( TerrainStorage ), 1 )]
	static void Upgrader_v1( JsonObject obj )
	{
		if ( obj["RootObject"] is not JsonObject root )
			return;

		var size = root["HeightMapSize"].Deserialize<int>();
		var heightmap = root["HeightMap"].Deserialize<string>();

		// I did pow2+1 heightmaps for a stupid reason, resample them to pow2
		if ( !BitOperations.IsPow2( size ) )
		{
			var data = TerrainMaps.Decompress<ushort>( Convert.FromBase64String( heightmap ) );
			var resized = ResampleHeightmap( data, size, size - 1 );
			heightmap = Convert.ToBase64String( TerrainMaps.Compress<ushort>( resized ) );
		}

		// These are still base64 deflate compressed
		var mapsObject = new JsonObject
		{
			["heightmap"] = heightmap,
			["splatmap"] = root["ControlMap"].Deserialize<JsonNode>(),
		};

		obj["Maps"] = mapsObject;
		obj["Resolution"] = size - 1;

		// There is no real way we can map the manual vtex layers to new materials
		// Sucks but its not like the control map is being wiped.
		obj["Materials"] = new JsonArray();

		obj["TerrainSize"] = root["TerrainSize"].Deserialize<JsonNode>();
		obj["TerrainHeight"] = root["TerrainHeight"].Deserialize<JsonNode>();

		// Remove old RootObject shite
		obj.Remove( "RootObject" );
	}

	static Span<ushort> ResampleHeightmap( Span<ushort> original, int originalSize, int newSize )
	{
		// Create SKBitmap with the original data copied in
		var bitmap = new SKBitmap( originalSize, originalSize, SKColorType.Alpha16, SKAlphaType.Opaque );
		using ( var pixmap = bitmap.PeekPixels() )
		{
			var dataBytes = MemoryMarshal.AsBytes( original );
			Marshal.Copy( dataBytes.ToArray(), 0, pixmap.GetPixels(), dataBytes.Length );
		}

		// Create new resized bitmap
		var newBitmap = bitmap.Resize( new SKSizeI( newSize, newSize ), SKSamplingOptions.Default );

		// Output pixels
		using ( var pixmap = newBitmap.PeekPixels() )
		{
			return pixmap.GetPixelSpan<ushort>();
		}
	}
}
