namespace Sandbox.Navigation;

public partial class NavMesh
{
	/// <summary>
	/// Implement this interface to receive navmesh editor events.
	/// </summary>
	public interface IEventListener
	{
		/// <summary>
		/// Called when an area definition has changed or loaded/created.
		/// </summary>
		void OnAreaDefinitionChanged() { }
	}
}
