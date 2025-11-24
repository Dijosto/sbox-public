using DotRecast.Detour;

namespace Sandbox.Navigation;

/// <summary>
/// Navigation Mesh - allowing AI to navigate a world
/// </summary>
public sealed partial class NavMesh
{
	public Vector3? GetRandomPoint()
	{
		// Can be null if called before Scene.NavMesh.Init has been called
		if ( query == null ) return null;

		var found = query.FindRandomPoint( DtQueryNoOpFilter.Shared, Random.Shared, out var poly, out var point );

		if ( found.Failed() ) return null;

		return FromNav( point );
	}

	/// <summary>
	/// Get a random point on the navmesh, within the bounding box. 
	/// This will return null if it can't find a point on the navmesh in a few tries. Returning false doesn't mean it's impossible, our algorithm here isn't the best.
	/// </summary>
	public Vector3? GetRandomPoint( BBox box )
	{
		if ( query == null ) return null;

		for ( int i = 0; i < 10; i++ )
		{
			var pos = box.RandomPointInside;
			var p = GetClosestPoint( pos );

			if ( p.HasValue && box.Contains( p.Value ) )
				return p.Value;
		}

		return null;
	}

	/// <summary>
	/// Get a random point on the navmesh, within the sphere.
	/// This will return null if it can't find a point on the navmesh in a few tries. Returning false doesn't mean it's impossible, our algorithm here isn't the best.
	/// </summary>
	public Vector3? GetRandomPoint( Vector3 position, float radius )
	{
		if ( query == null ) return null;

		var sphere = new Sphere( position, radius );

		for ( int i = 0; i < 10; i++ )
		{
			var pos = position + Random.Shared.VectorInSphere( radius );
			var p = GetClosestPoint( pos );

			if ( p.HasValue && sphere.Contains( p.Value ) )
				return p.Value;
		}

		return default;
	}

	public Vector3? GetClosestPoint( BBox box )
	{
		if ( query == null ) return null;

		var found = query.FindNearestPoly( ToNav( box.Center ), ToNav( box.Size / 2 ), DtQueryNoOpFilter.Shared, out var nearestRef, out var nearesPoint, out _ );

		if ( found.Failed() || nearestRef == 0 ) return null;

		return FromNav( nearesPoint );
	}

	public Vector3? GetClosestPoint( Vector3 position, float radius = 1024.0f ) => GetClosestPoint( BBox.FromPositionAndSize( position, radius * 2.0f ) );


	public Vector3? GetClosestEdge( BBox box )
	{
		if ( query == null ) return null;

		var foundPoly = query.FindNearestPoly( ToNav( box.Center ), ToNav( box.Size / 2 ), DtQueryNoOpFilter.Shared, out var nearestPoly, out var nearesPoint, out _ );

		if ( foundPoly.Failed() || nearestPoly == 0 ) return null;

		var found = query.FindDistanceToWall( nearestPoly, ToNav( box.Center ), box.Size.Length, DtQueryNoOpFilter.Shared, out var _, out var hitPos, out var _ );

		if ( found.Failed() ) return null;

		return FromNav( hitPos );
	}

	public Vector3? GetClosestEdge( Vector3 position, float radius = 1024.0f ) => GetClosestEdge( BBox.FromPositionAndSize( position, radius * 2.0f ) );
}
