namespace Sandbox;

internal sealed class VertexKDTree
{
	private class Node
	{
		public int[] Children = { -1, -1 };
		public int Axis = -1;
		public float Split;
		public int LeafStart, LeafCount;

		public bool IsLeaf => Axis == -1;
		public void InitAsSplit( float split, int axis ) => (Axis, Split) = (axis, split);
		public void InitAsLeaf( int start, int count ) => (Axis, LeafStart, LeafCount) = (-1, start, count);
	}

	private readonly List<Node> tree = new();
	private List<Vector3> vertexList;

	public void BuildMidpoint( List<Vector3> vertices )
	{
		vertexList = new( vertices );
		tree.Clear();
		BuildNode( 0, vertexList.Count );
	}

	private int BuildNode( int start, int count )
	{
		if ( count <= 8 )
		{
			var nodeIndex = tree.Count;
			tree.Add( new() );
			tree[nodeIndex].InitAsLeaf( start, count );
			return nodeIndex;
		}

		ComputeBounds( out var min, out var max, start, count );
		var axis = GreatestAxis( max - min );
		var split = (max[axis] + min[axis]) * 0.5f;
		var splitIndex = FindMidpointIndex( start, count, axis, split );

		if ( splitIndex == start || splitIndex == start + count )
		{
			var nodeIndex = tree.Count;
			tree.Add( new() );
			tree[nodeIndex].InitAsLeaf( start, count );
			return nodeIndex;
		}

		var nodeIdx = tree.Count;
		tree.Add( new() { Axis = axis, Split = split } );
		tree[nodeIdx].Children[0] = BuildNode( start, splitIndex - start );
		tree[nodeIdx].Children[1] = BuildNode( splitIndex, count - (splitIndex - start) );

		return nodeIdx;
	}

	private int FindMidpointIndex( int start, int count, int axis, float split )
	{
		vertexList.Sort( start, count, Comparer<Vector3>.Create( ( a, b ) => a[axis].CompareTo( b[axis] ) ) );
		var mid = start + count / 2;

		while ( mid < start + count && vertexList[mid][axis] < split ) mid++;
		while ( mid > start && vertexList[mid - 1][axis] >= split ) mid--;

		return mid;
	}

	private void ComputeBounds( out Vector3 min, out Vector3 max, int start, int count )
	{
		min = max = vertexList[start];
		for ( var i = start + 1; i < start + count; i++ )
		{
			min = Vector3.Min( min, vertexList[i] );
			max = Vector3.Max( max, vertexList[i] );
		}
	}

	private int GreatestAxis( Vector3 v ) => v.x >= v.y ? (v.x > v.z ? 0 : 2) : (v.y > v.z ? 1 : 2);

	public List<int> FindVertsInBox( Vector3 minBounds, Vector3 maxBounds )
	{
		var result = new List<int>();
		FindVertsInBoxRecursive( 0, minBounds, maxBounds, result );
		return result;
	}

	private void FindVertsInBoxRecursive( int nodeIndex, Vector3 minBounds, Vector3 maxBounds, List<int> result )
	{
		if ( nodeIndex < 0 || nodeIndex >= tree.Count ) return;

		var node = tree[nodeIndex];

		if ( node.IsLeaf )
		{
			for ( var i = node.LeafStart; i < node.LeafStart + node.LeafCount; i++ )
			{
				var p = vertexList[i];
				if ( p.x >= minBounds.y && p.x <= maxBounds.x &&
					p.y >= minBounds.y && p.y <= maxBounds.y &&
					p.z >= minBounds.z && p.z <= maxBounds.z )
				{
					result.Add( i );
				}
			}
		}
		else
		{
			var axis = node.Axis;
			if ( minBounds[axis] <= node.Split ) FindVertsInBoxRecursive( node.Children[0], minBounds, maxBounds, result );
			if ( maxBounds[axis] >= node.Split ) FindVertsInBoxRecursive( node.Children[1], minBounds, maxBounds, result );
		}
	}
}
