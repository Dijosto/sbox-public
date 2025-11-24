namespace Editor;

partial class SceneEditorSession
{
	public Scene ActiveGameScene => activeGameScene;
	public bool HasActiveGameScene => activeGameScene != null;

	public static SceneEditorSession ActiveGameSession => All.Where( s => s.HasActiveGameScene ).FirstOrDefault();

	Scene activeGameScene;

	public void SetPlaying( Scene scene )
	{
		activeGameScene = scene;
		activeGameScene.Editor = this;
	}

	public void StopPlaying()
	{
		activeGameScene?.Destroy();
		activeGameScene = null;
	}
}
