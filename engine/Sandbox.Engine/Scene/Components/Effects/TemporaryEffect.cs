
using Sandbox;

/// <summary>
/// Destroys a GameObject after a number of seconds. If the GameObject or its children have any 
/// components that implement ITemporaryEffect we will wait for those to be finished before destroying.
/// This is particularly useful if you want to delete a GameObject but want to wait for sounds or particles 
/// to conclude.
/// </summary>
[Expose]
[Title( "Temporary Effect" )]
[Category( "Effects" )]
[Icon( "history_toggle_off" )]
public sealed class TemporaryEffect : Component, Component.ExecuteInEditor
{
	/// <summary>
	/// Number of seconds to wait before destroying
	/// </summary>
	[Property] public float DestroyAfterSeconds = 1.0f;

	/// <summary>
	/// If true we will wait for any ITemporaryEffect's to finish before destroying
	/// </summary>
	[Property] public bool WaitForChildEffects = true;

	/// <summary>
	/// If the parent GameObject is destroyed we should become orphaned instead of being destroyed ourselves.
	/// Once orphaned we'll stop all looping effects and wait to die.
	/// </summary>
	[Property] public bool BecomeOrphan = false;

	TimeSince timeAlive;

	protected override void OnEnabled()
	{
		timeAlive = 0;
	}

	protected override void OnUpdate()
	{
		// If we're in the editor, only apply this logic
		// to objects that we are not saving.
		if ( Scene.IsEditor )
		{
			if ( !GameObject.HasFlagOrParent( GameObjectFlags.NotSaved ) && !GameObject.HasFlagOrParent( GameObjectFlags.Hidden ) )
				return;
		}

		if ( WaitForChildEffects && HasActiveEffects() )
		{
			return;
		}

		if ( timeAlive > DestroyAfterSeconds )
		{
			DestroyGameObject();
		}
	}

	bool HasActiveEffects()
	{
		foreach ( var pe in GetComponentsInChildren<ITemporaryEffect>() )
		{
			if ( pe.IsActive )
				return true;
		}

		return false;
	}

	public override void OnParentDestroy()
	{
		// don't call if we're shutting the scene down
		if ( Scene.IsDestroyed )
			return;

		// don't call if we're an editor scene
		if ( Scene.IsEditor )
			return;

		if ( BecomeOrphan )
		{
			CreateOrphans( GameObject, true );
		}
	}

	/// <summary>
	/// Look at the children in this GameObject and orphan any temporary effects
	/// </summary>
	public static void CreateOrphans( GameObject gameObject, bool disableLooping = true )
	{
		foreach ( var te in gameObject.GetComponentsInChildren<TemporaryEffect>().ToArray() )
		{
			te.GameObject.SetParent( null, true );

			if ( disableLooping )
			{
				ITemporaryEffect.DisableLoopingEffects( te.GameObject );
			}
		}
	}
}
