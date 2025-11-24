namespace Sandbox;

public abstract partial class Component
{
	/// <summary>
	/// Called once before the first Update - when enabled.
	/// </summary>
	protected virtual void OnStart() { }

	/// <summary>
	/// When enabled, called every frame
	/// </summary>
	protected virtual void OnUpdate() { }

	/// <summary>
	/// When enabled, called on a fixed interval that is determined by the Scene. This
	/// is also the fixed interval in which the physics are ticked. Time.Delta is that
	/// fixed interval.
	/// </summary>
	protected virtual void OnFixedUpdate() { }

	bool _startCalled;

	internal void InternalOnStart()
	{
		if ( !Enabled ) return;
		if ( !ShouldExecute ) return;

		if ( _startCalled ) return;

		// Disable any interpolation during OnStart. We might be created in a Fixed Update context.
		using ( GameTransform.DisableInterpolation() )
		{
			Scene.pendingStartComponents.Remove( this );
			_startCalled = true;
			ExceptionWrap( "Start", OnStart );

			if ( Scene is not null && !Scene.IsEditor )
			{
				ExceptionWrap( "Start", OnComponentStart );
			}
		}
	}

	internal virtual void InternalUpdate()
	{
		if ( !Enabled ) return;
		if ( !ShouldExecute ) return;

		InternalOnStart();
		ExceptionWrap( "Update", OnUpdate );

		if ( Scene is not null && !Scene.IsEditor )
		{
			ExceptionWrap( "Update", OnComponentUpdate );
		}
	}

	internal virtual void InternalFixedUpdate()
	{
		if ( !Enabled ) return;
		if ( !ShouldExecute ) return;

		InternalOnStart();
		ExceptionWrap( "FixedUpdate", OnFixedUpdate );

		if ( Scene is not null && !Scene.IsEditor )
		{
			ExceptionWrap( "FixedUpdate", OnComponentFixedUpdate );
		}
	}
}
