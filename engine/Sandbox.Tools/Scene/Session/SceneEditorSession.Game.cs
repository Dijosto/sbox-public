namespace Editor;

partial class SceneEditorSession
{
	/// <summary>
	/// The game session of this editor session, if playing.
	/// </summary>
	public GameEditorSession GameSession { get; private set; }

	public virtual bool IsPlaying => GameSession != null;

	public void SetPlaying( Scene scene )
	{
		// Clean up any existing game session before creating a new one
		if ( GameSession is not null )
		{
			StopPlaying();
		}

		GameSession = new GameEditorSession( this, scene );
		GameSession.MakeActive();
	}

	public virtual void StopPlaying()
	{
		GameSession?.Destroy();
		GameSession = null;

		MakeActive();
	}
}
