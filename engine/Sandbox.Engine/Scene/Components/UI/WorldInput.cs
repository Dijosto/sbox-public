using Sandbox.UI;

namespace Sandbox;

/// <summary>
/// A router for world input, the best place to put this is on your player's camera.
/// Uses cursor ray when mouse is active, otherwise the direction of this gameobject.
/// </summary>
[Title( "World Input" ), Category( "UI" ), Icon( "flip_camera_android" )]
public sealed class WorldInput : Component
{
	UI.WorldInput worldInput = new();

	/// <summary>
	/// Which action is our left clicking button?
	/// </summary>
	[Property, InputAction] public string LeftMouseAction { get; set; } = "Attack1";

	/// <summary>
	/// Which action is our right clicking button?
	/// </summary>
	[Property, InputAction] public string RightMouseAction { get; set; } = "Attack2";

	/// <summary>
	/// The <see cref="Panel"/> that is currently hovered by this input.
	/// </summary>
	public Panel Hovered => worldInput.Hovered;

	protected override void OnEnabled()
	{
		worldInput.Enabled = true;
	}

	protected override void OnDisabled()
	{
		worldInput.Enabled = false;
	}

	protected override void OnUpdate()
	{
		worldInput.Ray = Mouse.Active && Scene?.Camera is not null ?
			Scene.Camera.ScreenPixelToRay( Mouse.Position ) :
			new Ray( WorldPosition, WorldRotation.Forward );

		worldInput.MouseLeftPressed = Input.Down( LeftMouseAction );
		worldInput.MouseRightPressed = Input.Down( RightMouseAction );
		worldInput.MouseWheel = Input.MouseWheel;
	}
}
