using System.Buffers;

namespace Sandbox.Navigation.Generation;

[SkipHotload]
internal static class RegionBuilder
{
	[SkipHotload]
	private struct Region
	{
		public int SpanCount;
		public ushort Id;
		public int Area;
		public bool Remap;
		public bool Visited;
		public bool Overlap;
		public bool ConnectsToBorder;
		public int YMin, YMax;
		private int[] connectionsArr;
		private int[] floorsArr;
		private int connectionsCount;
		private int floorsCount;

		public ReadOnlySpan<int> Connections => connectionsArr.AsSpan( 0, connectionsCount );
		public ReadOnlySpan<int> Floors => floorsArr.AsSpan( 0, floorsCount );


		public Region( ushort id )
		{
			SpanCount = 0;
			Id = id;
			Area = Constants.NULL_AREA;
			Remap = false;
			Visited = false;
			Overlap = false;
			ConnectsToBorder = false;
			YMin = 0xffff;
			YMax = 0;
			EnsureConnectionCapacity( 16 );
			EnsureFloorCapacity( 8 );
		}

		private void EnsureConnectionCapacity( int targetConnectionCapacity )
		{
			if ( connectionsArr == null || connectionsArr.Length < targetConnectionCapacity )
			{
				var newConnectionsArr = ArrayPool<int>.Shared.Rent( targetConnectionCapacity * 2 );
				if ( connectionsArr != null )
				{
					Connections.CopyTo( newConnectionsArr );
					ArrayPool<int>.Shared.Return( connectionsArr );
				}
				connectionsArr = newConnectionsArr;
			}
		}

		private void EnsureFloorCapacity( int targetFloorCapacity )
		{
			if ( floorsArr == null || floorsArr.Length < targetFloorCapacity )
			{
				var newFloorsArr = ArrayPool<int>.Shared.Rent( targetFloorCapacity * 2 );
				if ( floorsArr != null )
				{
					Floors.CopyTo( newFloorsArr );
					ArrayPool<int>.Shared.Return( floorsArr );
				}
				floorsArr = newFloorsArr;
			}
		}

		public void AddUniqueFloorRegion( int n )
		{
			for ( int i = 0; i < Floors.Length; ++i )
				if ( Floors[i] == n )
					return;

			EnsureFloorCapacity( Floors.Length + 1 );
			floorsArr[floorsCount++] = n;
		}

		public void AddUniqueConnection( int n )
		{
			for ( int i = 0; i < Connections.Length; ++i )
				if ( Connections[i] == n )
					return;

			EnsureConnectionCapacity( Connections.Length + 1 );
			connectionsArr[connectionsCount++] = n;
		}

		public void Dispose()
		{
			if ( connectionsArr != null )
			{
				ArrayPool<int>.Shared.Return( connectionsArr );
				connectionsArr = null;
			}
			if ( floorsArr != null )
			{
				ArrayPool<int>.Shared.Return( floorsArr );
				floorsArr = null;
			}
			connectionsCount = 0;
			floorsCount = 0;
		}
	}

	[SkipHotload]
	public struct SweepSpan
	{
		public ushort Rid;   // Row id
		public ushort Id;    // Region id
		public ushort Ns;    // Number of samples
		public ushort Nei;   // Neighbor id
	}

	private static bool MergeAndFilterLayerRegions( int minRegionArea, ref ushort maxRegionId, CompactHeightfield chf, Span<ushort> srcReg )
	{
		int w = chf.Width;
		int h = chf.Height;

		int nreg = maxRegionId + 1;
		using var pooledRegions = new PooledSpan<Region>( nreg );
		Span<Region> regions = pooledRegions.Span;

		// Construct regions
		for ( int i = 0; i < nreg; ++i )
			regions[i] = new Region( (ushort)i );

		// Find region neighbors and overlapping regions
		List<int> lregs = new List<int>( 32 );
		for ( int y = 0; y < h; ++y )
		{
			for ( int x = 0; x < w; ++x )
			{
				CompactCell c = chf.Cells[x + y * w];

				lregs.Clear();

				for ( int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i )
				{
					CompactSpan s = chf.Spans[i];
					int area = chf.Areas[i];
					ushort ri = srcReg[i];
					if ( ri == 0 || ri >= nreg ) continue;

					ref Region reg = ref regions[ri];

					reg.SpanCount++;
					reg.Area = area;

					reg.YMin = Math.Min( reg.YMin, s.StartY );
					reg.YMax = Math.Max( reg.YMax, s.StartY );

					// Collect all region layers
					lregs.Add( ri );

					// Update neighbors
					for ( int dir = 0; dir < 4; ++dir )
					{
						if ( Utils.GetCon( s, dir ) != Constants.NOT_CONNECTED )
						{
							int ax = x + Utils.GetDirOffsetX( dir );
							int ay = y + Utils.GetDirOffsetZ( dir );
							int ai = (int)chf.Cells[ax + ay * w].Index + Utils.GetCon( s, dir );
							ushort rai = srcReg[ai];
							if ( rai > 0 && rai < nreg && rai != ri ) reg.AddUniqueConnection( rai );
							if ( (rai & ContourRegionFlags.BORDER_REG) != 0 ) reg.ConnectsToBorder = true;
						}
					}
				}

				// Update overlapping regions
				for ( int i = 0; i < lregs.Count - 1; ++i )
				{
					for ( int j = i + 1; j < lregs.Count; ++j )
					{
						if ( lregs[i] != lregs[j] )
						{
							ref Region ri = ref regions[lregs[i]];
							ref Region rj = ref regions[lregs[j]];
							ri.AddUniqueFloorRegion( lregs[j] );
							rj.AddUniqueFloorRegion( lregs[i] );
						}
					}
				}
			}
		}

		// Create 2D layers from regions
		ushort layerId = 1;

		for ( int i = 0; i < nreg; ++i )
			regions[i].Id = 0;

		// Merge monotone regions to create non-overlapping areas
		Queue<int> queue = new Queue<int>( 32 );
		for ( int i = 1; i < nreg; ++i )
		{
			ref Region root = ref regions[i];
			// Skip already visited.
			if ( root.Id != 0 )
			{
				continue;
			}

			// Start search.
			root.Id = layerId;

			queue.Clear();
			queue.Enqueue( i );

			while ( queue.Count > 0 )
			{
				// Pop front
				int idx = queue.Dequeue();
				ref readonly Region reg = ref regions[idx];

				int ncons = reg.Connections.Length;
				for ( int j = 0; j < ncons; ++j )
				{
					int nei = reg.Connections[j];
					ref Region regn = ref regions[nei];
					// Skip already visited.
					if ( regn.Id != 0 )
					{
						continue;
					}

					// Skip if different area type, do not connect regions with different area type.
					if ( reg.Area != regn.Area )
					{
						continue;
					}

					// Skip if the neighbour is overlapping root region.
					bool overlap = false;
					for ( int k = 0; k < root.Floors.Length; k++ )
					{
						if ( root.Floors[k] == nei )
						{
							overlap = true;
							break;
						}
					}

					if ( overlap )
					{
						continue;
					}

					// Deepen
					queue.Enqueue( nei );

					// Mark layer id
					regn.Id = layerId;
					// Merge current layers to root.
					for ( int k = 0; k < regn.Floors.Length; ++k )
					{
						root.AddUniqueFloorRegion( regn.Floors[k] );
					}

					root.YMin = Math.Min( root.YMin, regn.YMin );
					root.YMax = Math.Max( root.YMax, regn.YMax );
					root.SpanCount += regn.SpanCount;
					regn.SpanCount = 0;
					root.ConnectsToBorder = root.ConnectsToBorder || regn.ConnectsToBorder;
				}
			}

			layerId++;
		}

		// Remove small regions
		for ( int i = 0; i < nreg; ++i )
		{
			if ( regions[i].SpanCount > 0 && regions[i].SpanCount < minRegionArea && !regions[i].ConnectsToBorder )
			{
				ushort reg = regions[i].Id;
				for ( int j = 0; j < nreg; ++j )
					if ( regions[j].Id == reg )
						regions[j].Id = 0;
			}
		}

		// Compress region Ids
		for ( int i = 0; i < nreg; ++i )
		{
			regions[i].Remap = false;
			if ( regions[i].Id == 0 ) continue;              // Skip nil regions
			if ( (regions[i].Id & ContourRegionFlags.BORDER_REG) != 0 ) continue;   // Skip external regions
			regions[i].Remap = true;
		}

		ushort regIdGen = 0;
		for ( int i = 0; i < nreg; ++i )
		{
			if ( !regions[i].Remap )
				continue;
			ushort oldId = regions[i].Id;
			ushort newId = ++regIdGen;
			for ( int j = i; j < nreg; ++j )
			{
				if ( regions[j].Id == oldId )
				{
					regions[j].Id = newId;
					regions[j].Remap = false;
				}
			}
		}
		maxRegionId = regIdGen;

		// Remap regions
		for ( int i = 0; i < chf.SpanCount; ++i )
		{
			if ( (srcReg[i] & ContourRegionFlags.BORDER_REG) == 0 )
				srcReg[i] = regions[srcReg[i]].Id;
		}

		for ( int i = 0; i < nreg; ++i )
			regions[i].Dispose();

		return true;
	}

	private static void PaintRectRegion( int minx, int maxx, int miny, int maxy, ushort regId,
									  CompactHeightfield chf, Span<ushort> srcReg )
	{
		int w = chf.Width;
		for ( int y = miny; y < maxy; ++y )
		{
			for ( int x = minx; x < maxx; ++x )
			{
				CompactCell c = chf.Cells[x + y * w];
				for ( int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i )
				{
					if ( chf.Areas[i] != Constants.NULL_AREA )
						srcReg[i] = regId;
				}
			}
		}
	}

	public static bool BuildLayerRegions( CompactHeightfield chf, int borderSize, int minRegionArea, List<int> prevCache )
	{
		int w = chf.Width;
		int h = chf.Height;
		ushort id = 1;

		using var pooledSrcReg = new PooledSpan<ushort>( chf.SpanCount );
		var srcRegs = pooledSrcReg.Span;
		srcRegs.Fill( 0 );

		var nsweeps = Math.Max( chf.Width, chf.Height );
		using var pooledSweeps = new PooledSpan<SweepSpan>( nsweeps );
		var sweeps = pooledSweeps.Span;
		sweeps.Clear();

		// Mark border regions
		if ( borderSize > 0 )
		{
			// Make sure border will not overflow
			int bw = Math.Min( w, borderSize );
			int bh = Math.Min( h, borderSize );
			// Paint regions
			PaintRectRegion( 0, bw, 0, h, (ushort)(id | ContourRegionFlags.BORDER_REG), chf, srcRegs ); id++;
			PaintRectRegion( w - bw, w, 0, h, (ushort)(id | ContourRegionFlags.BORDER_REG), chf, srcRegs ); id++;
			PaintRectRegion( 0, w, 0, bh, (ushort)(id | ContourRegionFlags.BORDER_REG), chf, srcRegs ); id++;
			PaintRectRegion( 0, w, h - bh, h, (ushort)(id | ContourRegionFlags.BORDER_REG), chf, srcRegs ); id++;
		}

		chf.BorderSize = borderSize;

		// Sweep one line at a time
		for ( int y = borderSize; y < h - borderSize; ++y )
		{
			// Collect spans from this row
			prevCache.Clear();
			while ( prevCache.Count < id + 1 ) prevCache.Add( 0 );
			ushort rid = 1;

			for ( int x = borderSize; x < w - borderSize; ++x )
			{
				CompactCell c = chf.Cells[x + y * w];

				for ( int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i )
				{
					CompactSpan s = chf.Spans[i];
					if ( chf.Areas[i] == Constants.NULL_AREA ) continue;

					// -x
					ushort previd = 0;
					if ( Utils.GetCon( s, 0 ) != Constants.NOT_CONNECTED )
					{
						int ax = x + Utils.GetDirOffsetX( 0 );
						int ay = y + Utils.GetDirOffsetZ( 0 );
						int ai = (int)chf.Cells[ax + ay * w].Index + Utils.GetCon( s, 0 );
						if ( (srcRegs[ai] & ContourRegionFlags.BORDER_REG) == 0 && chf.Areas[i] == chf.Areas[ai] )
							previd = srcRegs[ai];
					}

					if ( previd == 0 )
					{
						previd = rid++;
						sweeps[previd].Rid = previd;
						sweeps[previd].Ns = 0;
						sweeps[previd].Nei = 0;
					}

					// -y
					if ( Utils.GetCon( s, 3 ) != Constants.NOT_CONNECTED )
					{
						int ax = x + Utils.GetDirOffsetX( 3 );
						int ay = y + Utils.GetDirOffsetZ( 3 );
						int ai = (int)chf.Cells[ax + ay * w].Index + Utils.GetCon( s, 3 );
						if ( srcRegs[ai] != 0 && (srcRegs[ai] & ContourRegionFlags.BORDER_REG) == 0 && chf.Areas[i] == chf.Areas[ai] )
						{
							ushort nr = srcRegs[ai];
							if ( sweeps[previd].Nei == 0 || sweeps[previd].Nei == nr )
							{
								sweeps[previd].Nei = nr;
								sweeps[previd].Ns++;
								prevCache[nr]++;
							}
							else
							{
								sweeps[previd].Nei = 0xffff; // RC_NULL_NEI
							}
						}
					}

					srcRegs[i] = previd;
				}
			}

			// Create unique ID
			for ( int i = 1; i < rid; ++i )
			{
				if ( sweeps[i].Nei != 0xffff && sweeps[i].Nei != 0 &&
					prevCache[sweeps[i].Nei] == sweeps[i].Ns )
				{
					sweeps[i].Id = sweeps[i].Nei;
				}
				else
				{
					sweeps[i].Id = id++;
				}
			}

			// Remap IDs
			for ( int x = borderSize; x < w - borderSize; ++x )
			{
				CompactCell c = chf.Cells[x + y * w];

				for ( int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i )
				{
					if ( srcRegs[i] > 0 && srcRegs[i] < rid )
						srcRegs[i] = sweeps[srcRegs[i]].Id;
				}
			}
		}

		// Merge monotone regions to layers and remove small regions
		chf.MaxRegions = id;
		if ( !MergeAndFilterLayerRegions( minRegionArea, ref chf.MaxRegions, chf, srcRegs ) )
		{
			return false;
		}

		// Store the result
		for ( int i = 0; i < chf.SpanCount; ++i ) chf.Spans[i].Region = srcRegs[i];

		return true;
	}
}
