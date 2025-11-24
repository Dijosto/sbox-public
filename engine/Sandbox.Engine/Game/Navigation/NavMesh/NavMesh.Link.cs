using Sandbox;
using Sandbox.Navigation;

internal class NavMeshLinkData : NavMeshSpatialAuxiliaryData
{
	public Vector3 StartPosition;
	public Vector3 EndPosition;

	public bool IsBiDirectional = true;

	public float ConnectionRadius;

	// Used to associate this object with recast data
	public object UserData = null;

	public bool IsStartConnected = false;

	public bool IsEndConnected = false;

	public Vector3 StartPositionOnNavMesh;

	public Vector3 EndPositionOnNavMesh;

	protected override RectInt CalculateCurrentOverlappingTiles( NavMesh navMesh )
	{
		// Create a box that encompasses both start and end positions with radius
		Vector3 min = Vector3.Min( StartPosition, EndPosition ) - new Vector3( ConnectionRadius );
		Vector3 max = Vector3.Max( StartPosition, EndPosition ) + new Vector3( ConnectionRadius );
		BBox linkBounds = new BBox( min, max );

		// Get all tiles that this bounds overlaps
		return navMesh.CalculateMinMaxTileCoords( linkBounds );
	}
}
