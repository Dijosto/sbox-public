namespace Sandbox;

public static class MenuCommands
{
	[MenuConCmd( "gameinfo", ConVarFlags.Protected )]
	public static void OpenCurrentGameDescription()
	{
		if ( string.IsNullOrEmpty( Application.GameIdent ) )
		{
			Log.Info( "Couldn't open gameinfo - not in a game." );
			return;
		}

		Log.Info( $"Opening game info for {Application.GameIdent}" );
		Game.Overlay.ShowPackageModal( Application.GameIdent );
	}
}
