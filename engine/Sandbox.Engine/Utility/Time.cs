using Sandbox.Utility;

namespace Sandbox;

public class Time
{
	/// <summary>
	/// The time since game startup
	/// </summary>
	public static float Now { get; set; }

	/// <summary>
	/// The delta between the last frame and the current (for all intents and purposes)
	/// </summary>
	public static float Delta { get; set; }


	// Audio.Time , Audio.TimeDelta - if these are needed

	//public static double Sound => g_pSoundSystem.AudioStateHostTime();

	//public static double SoundDelta => g_pSoundSystem.AudioStateFrameTime();

	internal static void Update( double now, double delta )
	{
		Now = (float)now;
		Delta = (float)delta;
	}

	public static IDisposable Scope( double now, double delta )
	{
		var d = Delta;
		var n = Now;

		Update( now, delta );

		return DisposeAction.Create( () =>
			{
				Delta = d;
				Now = n;
			} );
	}
}
