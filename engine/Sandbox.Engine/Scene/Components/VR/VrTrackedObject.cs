namespace Sandbox.VR;

/// <summary>
/// Updates this GameObject's transform based on a given tracked object (e.g. left controller, HMD).
/// </summary>
[Title( "VR Tracked Object" )]
[Category( "VR" )]
[EditorHandle( "materials/gizmo/tracked_object.png" )]
[Icon( "animation" )]
public class VRTrackedObject : Component
{
	/// <summary>
	/// Represents tracked devices to use when updating
	/// </summary>
	public enum PoseSources
	{
		/// <summary>
		/// Retrieve data from the head-mounted display
		/// </summary>
		Head,

		/// <summary>
		/// Retrieve data from the left controller
		/// </summary>
		LeftHand,

		/// <summary>
		/// Retrieve data from the right controller
		/// </summary>
		RightHand
	}

	/// <summary>
	/// Represents transform values to update
	/// </summary>
	[Flags]
	public enum TrackingTypes
	{
		/// <summary>
		/// Don't update the position or the rotation
		/// </summary>
		None,

		/// <summary>
		/// Update the rotation only
		/// </summary>
		Rotation,

		/// <summary>
		/// Update the rotation only
		/// </summary>
		Position,

		/// <summary>
		/// Update both the position and rotation
		/// </summary>
		All = Rotation | Position
	}

	/// <summary>
	/// Which tracked object should we use to update the transform?
	/// </summary>
	[Property]
	public PoseSources PoseSource { get; set; } = PoseSources.Head;

	/// <summary>
	/// Which parts of the transform should be updated? (eg. rotation, position)
	/// </summary>
	[Property]
	public TrackingTypes TrackingType { get; set; } = TrackingTypes.All;

	/// <summary>
	/// If this is checked, then the transform used will be relative to the VR anchor (rather than an absolute world position).
	/// </summary>
	[Property]
	public bool UseRelativeTransform { get; set; } = false;

	/// <summary>
	/// Get the appropriate VR transform for the specified <see cref="PoseSource"/>
	/// </summary>
	private Transform GetTransform()
	{
		return PoseSource switch
		{
			PoseSources.Head => Input.VR.Head,
			PoseSources.LeftHand => Input.VR.LeftHand.Transform,
			PoseSources.RightHand => Input.VR.RightHand.Transform,

			_ => new Transform( Vector3.Zero, Rotation.Identity )
		};
	}

	/// <summary>
	/// Set the GameObject's transform based on the <see cref="PoseSource"/> and <see cref="TrackingType"/>
	/// </summary>
	private void UpdatePose()
	{
		var newTransform = GetTransform();

		if ( UseRelativeTransform )
			newTransform = Input.VR.Anchor.ToLocal( newTransform );

		//
		// Update GameObject transform
		//
		if ( TrackingType.Contains( TrackingTypes.Position ) )
			GameObject.WorldPosition = newTransform.Position;

		if ( TrackingType.Contains( TrackingTypes.Rotation ) )
			GameObject.WorldRotation = newTransform.Rotation;
	}

	protected override void OnUpdate()
	{
		if ( !Enabled || Scene.IsEditor || !Game.IsRunningInVR )
			return;

		UpdatePose();
	}

	protected override void OnPreRender()
	{
		if ( !Enabled || Scene.IsEditor || !Game.IsRunningInVR )
			return;

		UpdatePose();
	}
}
