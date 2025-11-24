namespace Sandbox;

/// <summary>
/// Allows events before and after the the physics step
/// </summary>
public interface IScenePhysicsEvents : ISceneEvent<IScenePhysicsEvents>
{
	/// <summary>
	/// Called before the physics step is run. This is called pretty much
	/// right after FixedUpdate.
	/// </summary>
	void PrePhysicsStep() { }

	/// <summary>
	/// Called after the physics step is run
	/// </summary>
	void PostPhysicsStep() { }
}
