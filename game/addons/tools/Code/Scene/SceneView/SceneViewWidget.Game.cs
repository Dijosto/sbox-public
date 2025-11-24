namespace Editor;

public partial class SceneViewWidget
{
	public enum ViewMode
	{
		Scene,
		Game,
		GameEjected
	}

	public ViewMode CurrentView { get; private set; }
	private ViewMode lastView;

	private SceneViewportWidget _gameViewport;

	[Event( "scene.play" )]
	public void OnScenePlay()
	{
		if ( !Session.HasActiveGameScene ) return;
		CurrentView = ViewMode.Game;

		OnViewModeChanged();

		_gameViewport = _viewports.FirstOrDefault().Value;
		_gameViewport.StartPlay();
	}

	[Event( "scene.stop" )]
	public void OnSceneStop()
	{
		if ( !_gameViewport.IsValid() ) return;
		CurrentView = ViewMode.Scene;

		_gameViewport.StopPlay();
		_gameViewport = null;

		OnViewModeChanged();
	}

	public void ToggleEject()
	{
		if ( !Session.HasActiveGameScene ) return;

		CurrentView = CurrentView == ViewMode.Game ? ViewMode.GameEjected : ViewMode.Game;

		if ( CurrentView == ViewMode.Game )
		{
			_gameViewport.PossesGameCamera();
		}
		else if ( CurrentView == ViewMode.GameEjected )
		{
			_gameViewport.EjectGameCamera();
		}

		OnViewModeChanged();
	}

	/// <summary>
	/// Current view mode changed, we need to hide or show some UI things.
	/// </summary>
	void OnViewModeChanged()
	{
		_viewportTools.Rebuild();
		_sidePanel?.Visible = CurrentView != ViewMode.Game;
	}

	// The first viewport is our target for now - we could probably do something smarter
	// in future, like using the last focused viewport
	public SceneViewportWidget GetGameTarget()
	{
		return _gameViewport;
	}
}
