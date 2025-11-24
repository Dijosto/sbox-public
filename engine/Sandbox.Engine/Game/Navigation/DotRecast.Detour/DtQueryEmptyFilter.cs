namespace DotRecast.Detour
{
	internal class DtQueryEmptyFilter : IDtQueryFilter
	{
		public static readonly DtQueryEmptyFilter Shared = new DtQueryEmptyFilter();

		private DtQueryEmptyFilter()
		{
		}

		public bool PassFilter( long refs )
		{
			return false;
		}

		public float GetCost( Vector3 pa, Vector3 pb, long prevRef, long curRef, long nextRef )
		{
			return 0;
		}
	}
}
