namespace Sandbox;

/// <summary>
/// Draws anything
/// </summary>
internal class GizmoInlineSceneObject : SceneCustomObject
{
	public Action Action { get; set; }

	public GizmoInlineSceneObject( SceneWorld sceneWorld ) : base( sceneWorld )
	{
	}

	public override void RenderSceneObject()
	{
		Action?.Invoke();
	}
}
