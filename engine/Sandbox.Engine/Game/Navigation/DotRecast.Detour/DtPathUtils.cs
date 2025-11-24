/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
recast4j copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
DotRecast Copyright (c) 2023-2024 Choi Ikpil ikpil@naver.com
Copyright (c) 2024 Facepunch Studios Ltd

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:
1. The origin of this software must not be misrepresented; you must not
 claim that you wrote the original software. If you use this software
 in a product, an acknowledgment in the product documentation would be
 appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
 misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

namespace DotRecast.Detour
{
	internal static class DtPathUtils
	{
		public static bool InRange( Vector3 v1, Vector3 v2, float r, float h )
		{
			float dx = v2.x - v1.x;
			float dy = v2.y - v1.y;
			float dz = v2.z - v1.z;
			return (dx * dx + dz * dz) < r * r && MathF.Abs( dy ) < h;
		}

		public static int MergeCorridorStartMoved( ref List<long> path, int npath, int maxPath, Span<long> visited, int nvisited )
		{
			int furthestPath = -1;
			int furthestVisited = -1;

			// Find furthest common polygon.
			for ( int i = npath - 1; i >= 0; --i )
			{
				bool found = false;
				for ( int j = nvisited - 1; j >= 0; --j )
				{
					if ( path[i] == visited[j] )
					{
						furthestPath = i;
						furthestVisited = j;
						found = true;
					}
				}
				if ( found )
					break;
			}

			// If no intersection found just return current path.
			if ( furthestPath == -1 || furthestVisited == -1 )
				return npath;

			// Concatenate paths.

			// Adjust beginning of the buffer to include the visited.
			int req = nvisited - furthestVisited;
			int orig = Math.Min( furthestPath + 1, npath );
			int size = Math.Max( 0, npath - orig );

			if ( req + size > maxPath )
				size = maxPath - req;

			// Ensure path has enough capacity
			if ( path.Capacity < req + size )
				path.Capacity = req + size;

			// Ensure list is large enough
			while ( path.Count < req + size )
				path.Add( 0 );

			// Move existing elements (equivalent to memmove in C++)
			if ( size > 0 )
			{
				for ( int i = 0; i < size; i++ )
					path[req + i] = path[orig + i];
			}

			// Store visited
			for ( int i = 0; i < Math.Min( req, maxPath ); i++ )
				path[i] = visited[(nvisited - 1) - i];

			// Trim any excess elements
			if ( path.Count > req + size )
				path.RemoveRange( req + size, path.Count - (req + size) );

			return req + size;
		}

		public static int MergeCorridorEndMoved( ref List<long> path, int npath, int maxPath, Span<long> visited, int nvisited )
		{
			int furthestPath = -1;
			int furthestVisited = -1;

			// Find furthest common polygon.
			for ( int i = 0; i < npath; ++i )
			{
				bool found = false;
				for ( int j = nvisited - 1; j >= 0; --j )
				{
					if ( path[i] == visited[j] )
					{
						furthestPath = i;
						furthestVisited = j;
						found = true;
					}
				}
				if ( found )
					break;
			}

			// If no intersection found just return current path.
			if ( furthestPath == -1 || furthestVisited == -1 )
				return npath;

			// Concatenate paths.
			int ppos = furthestPath + 1;
			int vpos = furthestVisited + 1;
			int count = Math.Min( nvisited - vpos, maxPath - ppos );

			// Ensure path has enough capacity
			if ( path.Capacity < ppos + count )
				path.Capacity = ppos + count;

			// Resize list if needed
			while ( path.Count < ppos + count )
				path.Add( 0 );

			// Copy visited to path (equivalent to memcpy in C++)
			if ( count > 0 )
			{
				for ( int i = 0; i < count; i++ )
					path[ppos + i] = visited[vpos + i];
			}

			// Trim any excess elements
			if ( path.Count > ppos + count )
				path.RemoveRange( ppos + count, path.Count - (ppos + count) );

			return ppos + count;
		}

		public static int MergeCorridorStartShortcut( ref List<long> path, int npath, int maxPath, List<long> visited, int nvisited )
		{
			int furthestPath = -1;
			int furthestVisited = -1;

			// Find furthest common polygon.
			for ( int i = npath - 1; i >= 0; --i )
			{
				bool found = false;
				for ( int j = nvisited - 1; j >= 0; --j )
				{
					if ( path[i] == visited[j] )
					{
						furthestPath = i;
						furthestVisited = j;
						found = true;
					}
				}
				if ( found )
					break;
			}

			// If no intersection found just return current path.
			if ( furthestPath == -1 || furthestVisited == -1 )
				return npath;

			// Concatenate paths.

			// Adjust beginning of the buffer to include the visited.
			int req = furthestVisited;
			if ( req <= 0 )
				return npath;

			int orig = furthestPath;
			int size = Math.Max( 0, npath - orig );

			if ( req + size > maxPath )
				size = maxPath - req;

			// Ensure path has enough capacity
			if ( path.Capacity < req + size )
				path.Capacity = req + size;

			// Ensure list is large enough
			while ( path.Count < req + size )
				path.Add( 0 );

			// Move existing elements (equivalent to memmove in C++)
			if ( size > 0 )
			{
				for ( int i = 0; i < size; i++ )
					path[req + i] = path[orig + i];
			}

			// Store visited
			for ( int i = 0; i < req; i++ )
				path[i] = visited[i];

			// Trim any excess elements
			if ( path.Count > req + size )
				path.RemoveRange( req + size, path.Count - (req + size) );

			return req + size;
		}
	}
}
