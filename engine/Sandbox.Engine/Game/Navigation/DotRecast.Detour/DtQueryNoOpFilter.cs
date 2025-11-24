namespace DotRecast.Detour
{
	internal class DtQueryNoOpFilter : IDtQueryFilter
	{
		public static readonly DtQueryNoOpFilter Shared = new DtQueryNoOpFilter();

		private DtQueryNoOpFilter()
		{
		}

		public bool PassFilter( long refs )
		{
			return true;
		}

		public float GetCost( Vector3 pa, Vector3 pb, long prevRef, long curRef, long nextRef )
		{
			return 0;
		}
	}
}
