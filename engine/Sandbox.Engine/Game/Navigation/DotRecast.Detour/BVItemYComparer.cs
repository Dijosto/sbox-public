using System.Collections.Generic;

namespace DotRecast.Detour
{
	internal class BVItemYComparer : IComparer<BVItem>
	{
		public static readonly BVItemYComparer Shared = new BVItemYComparer();

		private BVItemYComparer()
		{
		}

		public int Compare( BVItem a, BVItem b )
		{
			return a.bmin.y.CompareTo( b.bmin.y );
		}
	}
}
