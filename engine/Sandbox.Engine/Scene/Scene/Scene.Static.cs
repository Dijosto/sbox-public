using Sandbox.Internal;

namespace Sandbox;

partial class Scene
{
	static WeakHashSet<Scene> _all { get; set; } = [];

	/// <summary>
	/// All active non-editor scenes.
	/// </summary>
	public static IEnumerable<Scene> All => _all.Where( x => !x.IsEditor && x.Active );
}
