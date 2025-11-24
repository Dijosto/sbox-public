using Sandbox.Engine;
using Sandbox.Internal;
using static System.Net.Mime.MediaTypeNames;

namespace Sandbox.Network;

internal partial class NetworkSystem
{
	public void UpdateLoading( string text )
	{
		LoadingScreen.IsVisible = true;
		LoadingScreen.Title = text;
	}
}


