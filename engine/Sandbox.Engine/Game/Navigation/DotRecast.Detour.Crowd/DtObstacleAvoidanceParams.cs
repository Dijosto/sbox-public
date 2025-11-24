namespace DotRecast.Detour.Crowd
{
	internal struct DtObstacleAvoidanceParams
	{
		/// <summary>
		/// Bias applied to favor the desired velocity directly; remaining portion defines exploration radius.
		/// Range [0,1]. Higher values stick closer to desired velocity before avoidance.
		/// </summary>
		public float VelocityBias = 0.4f;

		/// <summary>
		/// Weight applied to deviation from desired velocity (steering objective).
		/// Higher values prioritize matching the desired velocity.
		/// </summary>
		public float DesiredVelocityWeight = 2.0f;

		/// <summary>
		/// Weight applied to deviation from current velocity (inertia preservation).
		/// Higher values reduce abrupt changes in movement.
		/// </summary>
		public float CurrentVelocityWeight = 0.75f;

		/// <summary>
		/// Weight applied to the side bias heuristic (preference for passing direction).
		/// Helps pick routes that reduce future collisions.
		/// </summary>
		public float SideBiasWeight = 0.75f;

		/// <summary>
		/// Weight applied to time-of-impact (earlier collisions => higher penalty).
		/// Drives strong avoidance of imminent collisions.
		/// </summary>
		public float TimeOfImpactWeight = 2.5f;

		/// <summary>
		/// Time horizon (seconds) considered when predicting collisions.
		/// Longer horizons lead to more conservative velocity choices.
		/// </summary>
		public float HorizonTime = 2.5f;

		/// <summary>
		/// Resolution of the uniform grid sampler (number of cells per axis).
		/// Larger values increase sampling density and cost.
		/// </summary>
		public int GridResolution = 33;

		/// <summary>
		/// Number of angular divisions for adaptive sampling rings (pattern density).
		/// </summary>
		public int AdaptiveDivisions = 7;

		/// <summary>
		/// Number of concentric rings used in adaptive sampling.
		/// </summary>
		public int AdaptiveRings = 2;

		/// <summary>
		/// Number of refinement iterations performed during adaptive sampling.
		/// </summary>
		public int AdaptiveRefinementDepth = 5;

		public DtObstacleAvoidanceParams()
		{

		}
	};
}
