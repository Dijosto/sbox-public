using System.Buffers;

namespace Sandbox.Navigation.Generation;

[SkipHotload]
internal class ContourSet : IDisposable
{
	/// <summary>
	/// A list of the contours in the set.
	/// </summary>
	public List<Contour> Contours = new List<Contour>( 128 );

	/// <summary>
	/// The minimum bounds in world space. [(x, y, z)]
	/// </summary>
	public Vector3 BMin = new Vector3();

	/// <summary>
	/// The maximum bounds in world space. [(x, y, z)]
	/// </summary>
	public Vector3 BMax = new Vector3();

	/// <summary>
	/// The size of each cell. (On the xz-plane.)
	/// </summary>
	public float CellSize;

	/// <summary>
	/// The height of each cell. (The minimum increment along the y-axis.) */
	/// </summary>
	public float CellHeight;

	/// <summary>
	/// The width of the set. (Along the x-axis in cell units.)
	/// </summary>
	public int Width;

	/// <summary>
	/// The height of the set. (Along the z-axis in cell units.)
	/// </summary>
	public int Height;

	/// <summary>
	/// The AABB border size used to generate the source data from which the contours were derived.
	/// </summary>
	public int BorderSize;

	/// <summary>
	/// The max edge error that this contour set was simplified with.
	/// </summary>
	public float MaxError;

	public void Clear()
	{
		Contours.Clear();
	}

	public void Dispose()
	{
		foreach ( var c in Contours )
		{
			c.Dispose();
		}
	}
}

[SkipHotload]
internal class Contour : IDisposable
{
	public Span<int> Vertices => verticesArray.AsSpan( 0, VertexCount * 4 ); //< Simplified contour vertex and connection data. [Size: 4 * #nverts]
	private int[] verticesArray;
	public int VertexCount { get; private set; } //< The number of vertices in the simplified contour.
	public int Region; //< The region id of the contour.
	public int Area; //< The area id of the contour.

	public Contour( int vertexCount )
	{
		VertexCount = vertexCount;
		verticesArray = ArrayPool<int>.Shared.Rent( vertexCount * 4 );
	}

	public void Dispose()
	{
		if ( verticesArray != null )
		{
			ArrayPool<int>.Shared.Return( verticesArray );
			verticesArray = null;
		}
	}

	public static void MergeContours( Contour ca, Contour cb, int ia, int ib )
	{
		int maxVerts = ca.VertexCount + cb.VertexCount + 2;
		int[] vertsArr = ArrayPool<int>.Shared.Rent( maxVerts * 4 );

		int nv = 0;

		// Copy contour A.
		for ( int i = 0; i <= ca.VertexCount; ++i )
		{
			int dst = nv * 4;
			int src = ((ia + i) % ca.VertexCount) * 4;
			vertsArr[dst + 0] = ca.Vertices[src + 0];
			vertsArr[dst + 1] = ca.Vertices[src + 1];
			vertsArr[dst + 2] = ca.Vertices[src + 2];
			vertsArr[dst + 3] = ca.Vertices[src + 3];
			nv++;
		}

		// Copy contour B
		for ( int i = 0; i <= cb.VertexCount; ++i )
		{
			int dst = nv * 4;
			int src = ((ib + i) % cb.VertexCount) * 4;
			vertsArr[dst + 0] = cb.Vertices[src + 0];
			vertsArr[dst + 1] = cb.Vertices[src + 1];
			vertsArr[dst + 2] = cb.Vertices[src + 2];
			vertsArr[dst + 3] = cb.Vertices[src + 3];
			nv++;
		}

		if ( ca.verticesArray != null )
		{
			ArrayPool<int>.Shared.Return( ca.verticesArray );
		}

		ca.verticesArray = vertsArr;
		ca.VertexCount = nv;

		cb.Dispose();
		cb.VertexCount = 0;
	}
}
