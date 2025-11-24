namespace DotRecast.Detour
{
	internal class DtNoOpDtPolygonByCircleConstraint : IDtPolygonByCircleConstraint
	{
		public static readonly DtNoOpDtPolygonByCircleConstraint Shared = new DtNoOpDtPolygonByCircleConstraint();

		private DtNoOpDtPolygonByCircleConstraint()
		{
		}

		public int Apply( Span<Vector3> polyVerts, Vector3 circleCenter, float radius, out Span<Vector3> constrainedVerts )
		{
			constrainedVerts = polyVerts;
			return polyVerts.Length;
		}
	}
}
