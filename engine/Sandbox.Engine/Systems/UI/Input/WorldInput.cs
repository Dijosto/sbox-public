namespace Sandbox.UI;

/// <summary>
/// WorldInput can be used to simulate standard mouse inputs on WorldPanels.
/// </summary>
/// <remarks>
/// <para>
/// You need to set <see cref="Ray"/> and <see cref="MouseLeftPressed"/> to simulate inputs,
/// ideally this should be done in a BuildInput event.
/// </para>
/// </remarks>
public class WorldInput
{
	internal WorldInputInternal WorldInputInternal { get; init; } = new();

	/// <summary>
	/// This input won't tick when this is false.
	/// Any hovered panels will be cleared.
	/// </summary>
	public bool Enabled
	{
		get => WorldInputInternal.Enabled;
		set => WorldInputInternal.Enabled = value;
	}

	/// <summary>
	/// The Ray used to intersect with your world panels, simulating mouse position.
	/// </summary>
	/// <remarks>
	/// This should ideally be set in BuildInput or FrameSimulate.
	/// </remarks>
	public Ray Ray
	{
		get => WorldInputInternal.Ray;
		set => WorldInputInternal.Ray = value;
	}

	public bool MouseLeftPressed
	{
		get => WorldInputInternal.MouseLeftPressed;
		set => WorldInputInternal.MouseLeftPressed = value;
	}

	public bool MouseRightPressed
	{
		get => WorldInputInternal.MouseRightPressed;
		set => WorldInputInternal.MouseRightPressed = value;
	}

	/// <summary>
	/// Simulate the mouse scroll wheel.
	/// You could use <seealso cref="Sandbox.Input.MouseWheel"/>
	/// Or you could simulate it with the camera view delta for example.
	/// </summary>
	public Vector2 MouseWheel
	{
		get => WorldInputInternal.MouseWheel;
		set => WorldInputInternal.MouseWheel = value;
	}

	/// <summary>
	/// Instead of simulating mouse input, this will simply use the mouse input.
	/// </summary>
	public bool UseMouseInput
	{
		get => WorldInputInternal.UseMouseInput;
		set => WorldInputInternal.UseMouseInput = value;
	}

	/// <summary>
	/// The <see cref="Panel"/> that is currently hovered by this input.
	/// </summary>
	public Panel Hovered => WorldInputInternal.Hovered;

	/// <summary>
	/// The <see cref="Panel"/> that is currently pressed by this input.
	/// </summary>
	public Panel Active => WorldInputInternal.Active;
}
