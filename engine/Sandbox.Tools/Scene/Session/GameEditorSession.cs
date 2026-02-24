namespace Editor;

public class GameEditorSession : SceneEditorSession
{
	internal static GameEditorSession Current = null;

	public SceneEditorSession Parent { get; init; }

	public override bool IsPlaying => true;

	public GameEditorSession( SceneEditorSession parent, Scene scene ) : base( scene )
	{
		Parent = parent;

		// If a previous session wasn't properly cleaned up, destroy it first
		if ( Current is not null )
		{
			Log.Warning( "Destroying stale GameEditorSession before creating new one" );
			Current.Destroy();
		}

		Current = this;
	}

	public override void Destroy()
	{
		base.Destroy();

		Current = null;
	}

	public override void StopPlaying() => Parent.StopPlaying();

	public override void FrameTo( in BBox box )
	{
		Parent.FrameTo( box );
	}
}
