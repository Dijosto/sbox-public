namespace Editor;

/// <summary>
/// The scene dock is the actual tab that is shown in the editor. Its main
/// job is to host the SceneViewWidget and to switch the active session when
/// the dock is hovered or focused. It also destroys the session when the dock
/// is closed.
/// </summary>
public partial class SceneDock : Widget
{
	SceneEditorSession Session { get; set; }

	public SceneDock( SceneEditorSession session ) : base( null )
	{
		Session = session;

		Layout = Layout.Row();
		Layout.Add( new SceneViewWidget( session, this ) );
		DeleteOnClose = true;

		Name = session.Scene.Source?.ResourcePath;
	}

	protected override bool OnClose()
	{
		if ( Session.HasUnsavedChanges )
		{
			this.ShowUnsavedChangesDialog(
				assetName: Session.Scene.Name,
				assetType: Session.IsPrefabSession ? "prefab" : "scene",
				onSave: () => Session.Save( false ) );

			return false;
		}

		return true;
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		Session.Destroy();
		Session = null;
	}

	protected override void OnVisibilityChanged( bool visible )
	{
		base.OnVisibilityChanged( visible );

		if ( visible )
		{
			Session.MakeActive();
		}
	}

	protected override void OnFocus( FocusChangeReason reason )
	{
		base.OnFocus( reason );

		Session.MakeActive();
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		Session.MakeActive();
	}
}
