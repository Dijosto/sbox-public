using System.Collections.Generic;

namespace DotRecast.Detour
{
	internal class BVItemXComparer : IComparer<BVItem>
	{
		public static readonly BVItemXComparer Shared = new BVItemXComparer();

		private BVItemXComparer()
		{
		}

		public int Compare( BVItem a, BVItem b )
		{
			return a.bmin.x.CompareTo( b.bmin.x );
		}
	}
}
