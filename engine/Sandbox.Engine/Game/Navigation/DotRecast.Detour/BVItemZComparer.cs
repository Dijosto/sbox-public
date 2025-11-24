using System.Collections.Generic;

namespace DotRecast.Detour
{
	internal class BVItemZComparer : IComparer<BVItem>
	{
		public static readonly BVItemZComparer Shared = new BVItemZComparer();

		private BVItemZComparer()
		{
		}

		public int Compare( BVItem a, BVItem b )
		{
			return a.bmin.z.CompareTo( b.bmin.z );
		}
	}
}
