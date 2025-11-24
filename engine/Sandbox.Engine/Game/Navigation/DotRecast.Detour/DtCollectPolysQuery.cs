namespace DotRecast.Detour
{
	internal class DtCollectPolysQuery : IDtPolyQuery
	{
		private long[] m_polys;
		private int m_maxPolys;
		private int m_numCollected;
		private bool m_overflow;

		public DtCollectPolysQuery( long[] polys, int maxPolys )
		{
			m_polys = polys;
			m_maxPolys = maxPolys;
		}

		public int NumCollected()
		{
			return m_numCollected;
		}

		public bool Overflowed()
		{
			return m_overflow;
		}

		public void Process( DtMeshTile tile, Span<long> refs, int count )
		{
			int numLeft = m_maxPolys - m_numCollected;
			int toCopy = count;
			if ( toCopy > numLeft )
			{
				m_overflow = true;
				toCopy = numLeft;
			}

			refs.Slice( 0, toCopy ).CopyTo( m_polys.AsSpan( m_numCollected, toCopy ) );
			m_numCollected += toCopy;
		}
	}
}
