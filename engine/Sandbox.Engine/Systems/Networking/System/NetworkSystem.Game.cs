using Sandbox.Engine;

namespace Sandbox.Network;


internal partial class NetworkSystem
{
	internal GameNetworkSystem GameSystem { get; set; }

	public void InitializeGameSystem()
	{
		if ( IGameInstanceDll.Current is null )
			return;

		GameSystem = IGameInstanceDll.Current.CreateGameNetworking( this );
		GameSystem?.OnInitialize();

		if ( GameSystem is null )
			Disconnect();
	}
}
