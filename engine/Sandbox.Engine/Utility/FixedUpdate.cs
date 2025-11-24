namespace Sandbox;

internal class FixedUpdate
{
	/// <summary>
	/// How many times a second FixedUpdate runs
	/// </summary>
	public float Frequency = 16;

	public float Delta => 1.0f / Frequency;

	/// <summary>
	/// Accumulate frame time up until a maximum amount (maxSteps). While this value
	/// is above the <see cref="Delta"/> time we will invoke a fixed update.
	/// </summary>
	private long step;

	internal void Run( Action fixedUpdate, float time, int maxSteps )
	{
		var delta = Delta;
		long curStep = (long)Math.Floor( time / delta );

		// Clamp the steps so we never jump too many
		step = long.Clamp( step, curStep - maxSteps, curStep );

		if ( step == curStep )
			return;

		while ( step < curStep )
		{
			step++;
			using var timeScope = Time.Scope( (step * delta), delta );
			fixedUpdate();
		}

		// always end up to date
		step = curStep;

	}
}
