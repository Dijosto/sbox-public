using Editor;

namespace Sandbox;

public partial class StartupWindow : Window
{
	private Vector2 WindowSize => new Vector2( 600, 600 );

	private Layout Body { get; set; }

	private Toggle CloseOnLaunch { get; set; }

	private HomeWidget HomeView { get; set; }
	private GameBrowserWidget BrowseView { get; set; }

	private SidebarButton MyProjectsTab { get; set; }
	private SidebarButton BrowseGamesTab { get; set; }

	public StartupWindow()
	{
		Canvas = new Widget( this );

		Size = WindowSize;
		MaximumSize = WindowSize;
		MinimumSize = WindowSize;
		HasMaximizeButton = false;
		Visible = false;

		WindowTitle = "Welcome to the s&box editor";

		SetWindowIcon( Pixmap.FromFile( "logo_rounded.png" ) );

		CreateUI();

		StatusBar.Destroy();
	}

	public override void Show()
	{
		base.Show();

		RestoreGeometry( LauncherPreferences.Cookie.Get( "startscreen.geometry", "" ) );
	}

	protected override bool OnClose()
	{
		EditorCookie = null;

		LauncherPreferences.Cookie.Set( "startscreen.geometry", SaveGeometry() );

		return base.OnClose();
	}

	private void CreateUI()
	{
		Canvas.Layout = Layout.Row();

		//
		// Sidebar
		//
		{
			var sidebar = Canvas.Layout.Add( new SidebarWidget( Canvas ), 1 );

			{
				var heading = sidebar.Add( new Widget( Canvas ) { FixedHeight = 32 } );
				heading.Layout = Layout.Row();

				var headingRow = heading.Layout;
				headingRow.Add( new LogoWidget( Canvas ) );
			}

			sidebar.AddSpacer();

			//
			// Tab navigation
			//
			{
				MyProjectsTab = sidebar.Add( new SidebarButton( "My Projects", "folder_open", () => ShowView( "home" ) ) );
				BrowseGamesTab = sidebar.Add( new SidebarButton( "Browse Games", "travel_explore", () => ShowView( "browse" ) ) );
			}

			sidebar.AddSpacer();

			//
			// Links
			//
			{
				sidebar.Add( new SidebarButton( "Documentation", "school", "https://sbox.game/dev/doc/" ) );
				sidebar.Add( new SidebarButton( $"Open {Global.BackendTitle}", "celebration", Global.BackendUrl ) );
				sidebar.Add( new SidebarButton( "API Reference", "code", $"{Global.BackendUrl}/api" ) );
			}

			sidebar.AddSpacer();

			//
			// Development
			//
			{
				var gameFolder = Environment.CurrentDirectory;

				sidebar.Add( new SidebarButton( "Engine Folder", "folder", gameFolder ) { IsExternal = false } );
				sidebar.Add( new SidebarButton( "Logs", "density_small", $"{gameFolder}/logs" ) { IsExternal = false } );
			}

			sidebar.AddStretchCell();

			CloseOnLaunch = sidebar.Add( new Toggle( "Close On Launch" ) );
			CloseOnLaunch.Value = LauncherPreferences.CloseOnLaunch;
			CloseOnLaunch.ValueChanged += ( v ) =>
			{
				LauncherPreferences.CloseOnLaunch = v;
			};
		}

		//
		// Body
		//
		{
			Body = Canvas.Layout.AddColumn( 3 );

			HomeView = new HomeWidget( Canvas );
			Body.Add( HomeView, 1 );

			BrowseView = new GameBrowserWidget( Canvas );
			BrowseView.Visible = false;
			BrowseView.OnBackRequested = () => ShowView( "home" );
			BrowseView.OnForkRequested = OnForkGameRequested;
			Body.Add( BrowseView, 1 );
		}
	}

	void ShowView( string view )
	{
		HomeView.Visible = view == "home";
		BrowseView.Visible = view == "browse";
	}

	void OnForkGameRequested( Package game )
	{
		var dialog = new GameForkDialog( game );

		dialog.OnProjectCreated = configPath =>
		{
			// Add to project list and open
			HomeView.AddForkedProject( configPath );
			ShowView( "home" );
		};

		dialog.Show();
	}

	public void OnSuccessfulLaunch()
	{
		if ( !CloseOnLaunch.Value ) return;

		Destroy();
	}
}
