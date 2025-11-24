namespace DotRecast.Detour
{
	// Calculate the intersection between a polygon and a circle. A dodecagon is used as an approximation of the circle.
	internal class DtStrictDtPolygonByCircleConstraint : IDtPolygonByCircleConstraint
	{
		private const int CIRCLE_SEGMENTS = 12;
		private static readonly Vector3[] UnitCircle = CreateCircle();

		public static readonly IDtPolygonByCircleConstraint Shared = new DtStrictDtPolygonByCircleConstraint();

		private DtStrictDtPolygonByCircleConstraint()
		{
		}

		public static Vector3[] CreateCircle()
		{
			var temp = new Vector3[CIRCLE_SEGMENTS];
			for ( int i = 0; i < CIRCLE_SEGMENTS; i++ )
			{
				float a = i * MathF.PI * 2 / CIRCLE_SEGMENTS;
				temp[i].x = MathF.Cos( a );
				temp[i].y = 0;
				temp[i].z = -MathF.Sin( a );
			}

			return temp;
		}

		public static void ScaleCircle( Span<Vector3> src, Vector3 center, float radius, Span<Vector3> dst )
		{
			for ( int i = 0; i < CIRCLE_SEGMENTS; i++ )
			{
				dst[i].x = src[i].x * radius + center.x;
				dst[i].y = center.y;
				dst[i].z = src[i].z * radius + center.z;
			}
		}


		public int Apply( Span<Vector3> verts, Vector3 center, float radius, out Span<Vector3> constrainedVerts )
		{
			float radiusSqr = radius * radius;
			int outsideVertex = -1;
			for ( int pv = 0; pv < verts.Length; ++pv )
			{
				if ( DtUtils.DistanceBetween2DSqr( center, verts[pv] ) > radiusSqr )
				{
					outsideVertex = pv;
					break;
				}
			}

			if ( outsideVertex == -1 )
			{
				// polygon inside circle
				constrainedVerts = verts;
				return verts.Length;
			}

			Span<Vector3> qCircle = stackalloc Vector3[UnitCircle.Length];
			ScaleCircle( UnitCircle, center, radius, qCircle );
			Vector3[] intersection = DtConvexConvexIntersections.Intersect( verts, qCircle );
			if ( intersection == null && DtUtils.PointInPolygon( center, verts ) )
			{
				// circle inside polygon
				Vector3[] qCircleArray = qCircle.ToArray();
				constrainedVerts = qCircleArray;
				return qCircleArray.Length;
			}

			constrainedVerts = intersection;
			return intersection?.Length ?? 0;
		}
	}
}
