namespace DotRecast.Detour
{
	internal readonly struct DtPolyPoint
	{
		public readonly long refs;
		public readonly Vector3 pt;

		public DtPolyPoint( long polyRefs, Vector3 polyPt )
		{
			refs = polyRefs;
			pt = polyPt;
		}
	}
}
