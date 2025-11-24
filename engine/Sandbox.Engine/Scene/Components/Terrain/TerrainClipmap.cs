using System.Buffers;
using System.Runtime.InteropServices;

namespace Sandbox;

internal static class TerrainClipmap
{
	[StructLayout( LayoutKind.Sequential )]
	public struct PosAndLodVertex
	{
		public PosAndLodVertex( Vector3 position )
		{
			this.position = position;
		}

		public Vector3 position;

		public static readonly VertexAttribute[] Layout =
		{
			new VertexAttribute( VertexAttributeType.Position, VertexAttributeFormat.Float32, 3 ),
		};
	}

	public static Mesh GenerateMesh( int LodLevels, int LodExtentTexels, Material material )
	{
		var vertices = new List<PosAndLodVertex>( 32 );
		var indices = new List<int>();

		// Loop through each LOD level
		for ( int level = 0; level < LodLevels; level++ )
		{
			int step = 1 << level;
			int prevStep = Math.Max( 0, 1 << (level - 1) );

			int g = LodExtentTexels / 2;

			int pad = 1;
			int radius = step * (g + pad);

			for ( int y = -radius; y < radius; y += step )
			{
				for ( int x = -radius; x < radius; x += step )
				{
					if ( Math.Max( Math.Abs( x + prevStep ), Math.Abs( y + prevStep ) ) < (g * prevStep) )
						continue;

					vertices.Add( new PosAndLodVertex( new Vector3( x, y, level ) ) );
					vertices.Add( new PosAndLodVertex( new Vector3( x + step, y, level ) ) );
					vertices.Add( new PosAndLodVertex( new Vector3( x + step, y + step, level ) ) );
					vertices.Add( new PosAndLodVertex( new Vector3( x, y + step, level ) ) );

					indices.Add( vertices.Count - 4 );
					indices.Add( vertices.Count - 3 );
					indices.Add( vertices.Count - 2 );
					indices.Add( vertices.Count - 2 );
					indices.Add( vertices.Count - 1 );
					indices.Add( vertices.Count - 4 );
				}
			}
		}

		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer( vertices.Count, PosAndLodVertex.Layout, vertices );
		mesh.CreateIndexBuffer( indices.Count, indices );
		return mesh;
	}

	/// <summary>
	/// Inefficient implementation of diamond square, it's not merging verticies.
	/// </summary>
	/// <returns></returns>
	public static Mesh GenerateMesh_DiamondSquare( int LodLevels, int LodExtentTexels, Material material, int subdivisionFactor = 1, int subdivisionLodCount = 3 )
	{
		var total = LodLevels * 36 * (LodExtentTexels / 2 + 1) * (LodExtentTexels / 2 + 1) * subdivisionFactor * subdivisionFactor;

		var vertices = ArrayPool<PosAndLodVertex>.Shared.Rent( total );
		var indices = ArrayPool<int>.Shared.Rent( total * 3 );

		int vertex = 0;
		int idx = 0;

		// Loop through each LOD level
		for ( int level = 0; level < LodLevels; level++ )
		{
			int lodBaseStep = 1 << level;

			// We only subdivise LOD levels athat are < than subDivisionLodCount
			int currentSubdivision = level < subdivisionLodCount ? subdivisionFactor : 1;
			float step = (float)lodBaseStep / currentSubdivision;

			int g = LodExtentTexels / 2;
			int pad = 1;

			int radius = lodBaseStep * (g + pad);
			int prevLodBaseStep = level > 0 ? (1 << (level - 1)) : 0;
			int innerRadius = prevLodBaseStep * g;

			for ( float y = -radius; y < radius; y += step )
			{
				for ( float x = -radius; x < radius; x += step )
				{
					if ( Math.Max( Math.Abs( x ), Math.Abs( y ) ) < innerRadius )
						continue;

					//   A-----B-----C
					//   | \   |   / |
					//   |   \ | /   |
					//   D-----E-----F
					//   |   / | \   |
					//   | /   |   \ |
					//   G-----H-----I

					var vecA = new Vector3( x, y, level );
					var vecC = new Vector3( x + step, y, level );
					var vecG = new Vector3( x, y + step, level );
					var vecI = new Vector3( x + step, y + step, level );
					var vecB = (vecA + vecC) * 0.5f;
					var vecD = (vecA + vecG) * 0.5f;
					var vecF = (vecC + vecI) * 0.5f;
					var vecH = (vecG + vecI) * 0.5f;
					var vecE = (vecA + vecI) * 0.5f;

					vertices[vertex++].position = vecA;
					vertices[vertex++].position = vecB;
					vertices[vertex++].position = vecC;
					vertices[vertex++].position = vecD;
					vertices[vertex++].position = vecE;
					vertices[vertex++].position = vecF;
					vertices[vertex++].position = vecG;
					vertices[vertex++].position = vecH;
					vertices[vertex++].position = vecI;

					// Stitch the border into the next level
					if ( x == -radius )
					{
						// E G A
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 3;
						indices[idx++] = vertex - 9;
					}
					else
					{
						// E D A
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 6;
						indices[idx++] = vertex - 9;
						// E G D
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 3;
						indices[idx++] = vertex - 6;
					}

					if ( y == radius - 1 )
					{
						// E I G
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 1;
						indices[idx++] = vertex - 3;
					}
					else
					{
						// E H G
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 2;
						indices[idx++] = vertex - 3;
						// E I H
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 1;
						indices[idx++] = vertex - 2;
					}

					if ( x == radius - 1 )
					{
						// E C I
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 7;
						indices[idx++] = vertex - 1;
					}
					else
					{
						// E F I
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 4;
						indices[idx++] = vertex - 1;
						// E C F
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 7;
						indices[idx++] = vertex - 4;
					}

					if ( y == -radius )
					{
						// E A C
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 9;
						indices[idx++] = vertex - 7;
					}
					else
					{
						// E B C
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 8;
						indices[idx++] = vertex - 7;
						// E A B
						indices[idx++] = vertex - 5;
						indices[idx++] = vertex - 9;
						indices[idx++] = vertex - 8;
					}
				}
			}
		}

		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer( vertex, PosAndLodVertex.Layout, vertices.AsSpan() );
		mesh.CreateIndexBuffer( idx, indices.AsSpan() );

		ArrayPool<PosAndLodVertex>.Shared.Return( vertices );
		ArrayPool<int>.Shared.Return( indices );

		return mesh;
	}
}
