namespace Sandbox;

public partial class Component
{
	protected virtual Task OnLoad()
	{
		return Task.CompletedTask;
	}

	private void LaunchLoader()
	{
		var loadingTask = OnLoad();
		if ( loadingTask is null || loadingTask.IsCompletedSuccessfully )
			return;

		GameObject.Flags |= GameObjectFlags.Loading;
		Scene.AddLoadingTask( WaitForLoad( loadingTask ) );

	}

	private async Task WaitForLoad( Task task )
	{
		await task;

		if ( !this.IsValid() ) return;
		if ( !GameObject.IsValid() ) return;

		GameObject.Flags &= ~GameObjectFlags.Loading;
	}

	internal void OnLoadInternal()
	{
		CallbackBatch.Add( CommonCallback.Loading, LaunchLoader, this, "LaunchLoader" );
	}
}
