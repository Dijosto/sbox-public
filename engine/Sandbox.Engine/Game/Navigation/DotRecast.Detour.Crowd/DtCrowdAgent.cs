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

namespace DotRecast.Detour.Crowd
{
	/// Represents an agent managed by a #dtCrowd object.
	/// @ingroup crowd
	internal class DtCrowdAgent
	{
		public readonly int idx;

		/// The type of mesh polygon the agent is traversing. (See: #CrowdAgentState)
		public DtCrowdAgentState state;

		/// True if the agent has valid path (targetState == DT_CROWDAGENT_TARGET_VALID) and the path does not lead to the requested position, else false.
		public bool partial;

		/// The path corridor the agent is using.
		public readonly DtPathCorridor corridor;

		/// The local boundary data for the agent.
		public readonly DtLocalBoundary boundary;

		/// Time since the agent's path corridor was optimized.
		public float timeSinceTopologyOptimization;

		/// The known neighbors of the agent.
		public readonly DtCrowdNeighbour[] neis = new DtCrowdNeighbour[DtCrowdConst.DT_CROWDAGENT_MAX_NEIGHBOURS];

		/// The number of neighbors.
		public int nneis;

		/// The desired speed.
		public float desiredSpeed;

		public Vector3 npos = new Vector3(); // < The current agent position.
		public Vector3 disp = new Vector3(); // < A temporary value used to accumulate agent displacement during iterative collision resolution.
		public Vector3 dvel = new Vector3(); // < The desired velocity of the agent. Based on the current path, calculated from scratch each frame.
		public Vector3 nvel = new Vector3(); // < The desired velocity adjusted by obstacle avoidance, calculated from scratch each frame.
		public Vector3 vel = new Vector3(); // < The actual velocity of the agent. The change from nvel -> vel is constrained by max acceleration.

		/// The agent's configuration parameters.
		public DtCrowdAgentParams option;

		/// The local path corridor corners for the agent.
		public DtStraightPath[] corners = new DtStraightPath[DtCrowdConst.DT_CROWDAGENT_MAX_CORNERS];

		/// The number of corners.
		public int ncorners;

		public DtMoveRequestState targetState; // < State of the movement request.
		public long targetRef; // < Target polyref of the movement request.
		public Vector3 targetPos = new Vector3(); // < Target position of the movement request (or velocity in case of DT_CROWDAGENT_TARGET_VELOCITY).
		public DtPathQueryResult targetPathQueryResult; // < Path finder query
		public bool targetReplan; // < Flag indicating that the current path is being replanned.
		public float timeSinceLastTargetReplan; // <Time since the agent's target was replanned.
		public float targetReplanWaitTime;
		public float timeSinceLastRecoveryCheck; // < Time since the last path validation because of an invalid position

		public DtCrowdAgentAnimation animation;

		public DtCrowdAgent( int idx )
		{
			this.idx = idx;
			corridor = new DtPathCorridor();
			boundary = new DtLocalBoundary();
			animation = new DtCrowdAgentAnimation();
		}

		public void Integrate( float dt )
		{
			// Fake dynamic constraint.
			float maxDelta = option.maxAcceleration * dt;
			Vector3 dv = nvel - vel;
			float ds = dv.Length;
			if ( ds > maxDelta )
				dv = dv * (maxDelta / ds);
			vel = vel + dv;

			// Integrate
			if ( vel.Length > 0.0001f )
				npos = npos + vel * dt;
			else
				vel = Vector3.Zero;
		}

		public bool OverOffmeshConnection( float radius )
		{
			if ( 0 == ncorners )
				return false;

			bool offMeshConnection = ((corners[ncorners - 1].flags
									   & DtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION) != 0)
				? true
				: false;
			if ( offMeshConnection )
			{
				float distSq = DtUtils.DistanceBetween2DSqr( npos, corners[ncorners - 1].pos );
				if ( distSq < radius * radius )
					return true;
			}

			return false;
		}

		public float GetDistanceToGoal( float range )
		{
			if ( 0 == ncorners )
				return range;

			bool endOfPath = ((corners[ncorners - 1].flags & DtStraightPathFlags.DT_STRAIGHTPATH_END) != 0) ? true : false;
			if ( endOfPath )
				return Math.Min( DtUtils.DistanceBetween2D( npos, corners[ncorners - 1].pos ), range );

			return range;
		}

		public Vector3 CalcSmoothSteerDirection()
		{
			Vector3 dir = new Vector3();
			if ( 0 < ncorners )
			{
				int ip0 = 0;
				int ip1 = Math.Min( 1, ncorners - 1 );
				var p0 = corners[ip0].pos;
				var p1 = corners[ip1].pos;

				var dir0 = p0 - npos;
				var dir1 = p1 - npos;
				dir0.y = 0;
				dir1.y = 0;

				float len0 = dir0.Length;
				float len1 = dir1.Length;
				if ( len1 > 0.001f )
					dir1 = dir1 * (1.0f / len1);

				// Only start smoothing when we're close to the turn.
				// Smooth steering looks dumb with long straight segments.
				// And may even cause issues when smooth steering is fihting with avoidance.
				float baseDistance = option.radius * 10.0f;
				float smoothFactor;

				if ( len0 > 0.001f )
				{
					// Calculate normalized distance ratio (higher when farther from corner)
					float distanceRatio = len0 / baseDistance;

					// Apply inverse-square falloff for more natural steering
					// This creates a smoothing factor that:
					// - Approaches 0.5 when very close to the corner
					// - Approaches 0 when very far from the corner
					smoothFactor = 0.5f / (1.0f + distanceRatio * distanceRatio);
				}
				else
				{
					// At the corner, use maximum smoothing
					smoothFactor = 0.5f;
				}

				dir.x = dir0.x - dir1.x * len0 * smoothFactor;
				dir.y = 0;
				dir.z = dir0.z - dir1.z * len0 * smoothFactor;
				dir = dir.Normal;
			}

			return dir;
		}

		public Vector3 CalcStraightSteerDirection()
		{
			Vector3 dir = new Vector3();
			if ( 0 < ncorners )
			{
				dir = corners[0].pos - npos;
				dir.y = 0;
				dir = dir.Normal;
			}

			return dir;
		}

		public void SetTarget( long refs, Vector3 pos )
		{
			targetRef = refs;
			targetPos = pos;
			targetPathQueryResult = null;
			if ( targetRef != 0 )
			{
				targetState = DtMoveRequestState.DT_CROWDAGENT_TARGET_REQUESTING;
			}
			else
			{
				targetState = DtMoveRequestState.DT_CROWDAGENT_TARGET_FAILED;
			}
		}
	}
}
