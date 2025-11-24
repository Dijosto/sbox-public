using Sandbox.Engine;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sandbox;

public class StandaloneAppSystem : AppSystem
{
	private StandaloneManifest LoadManifest()
	{
		// Load game info from file
		var manifestPath = Path.Combine( Standalone.GamePath, Standalone.ManifestName );
		var manifestContents = File.ReadAllText( manifestPath );
		var properties = JsonSerializer.Deserialize<StandaloneManifest>( manifestContents );

		return properties;
	}

	public override void Init()
	{
		LoadSteamDll();

		base.Init();

		// Standalone setup
		Standalone.SetupFromManifest( LoadManifest() );

		Application.IsStandalone = true;
		Application.AppId = Standalone.Manifest.AppId;

		CreateGame();

		var createInfo = new AppSystemCreateInfo()
		{
			WindowTitle = Standalone.Manifest.Name,
			Flags = AppSystemFlags.IsGameApp | AppSystemFlags.IsStandaloneGame
		};

		if ( Utility.CommandLine.HasSwitch( "-headless" ) )
			createInfo.Flags |= AppSystemFlags.IsConsoleApp;

		InitGame( createInfo );

		LoadStandaloneGame();
	}

	private void LoadStandaloneGame()
	{
		IGameInstanceDll.Current.LoadGamePackageAsync( Standalone.Manifest.Ident, GameLoadingFlags.Host, new() );
	}
}
