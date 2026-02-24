using Sandbox;
using Sandbox.Diagnostics;
using System.Threading;

namespace Editor;

/// <summary>
/// Widget that displays a browsable grid of cloud game packages.
/// Users can search, browse, and select a game to fork into a local project.
/// </summary>
public class GameBrowserWidget : Widget
{
	static Logger Log = new( "GameBrowser" );

	private string _searchText = "";
	private Layout GameGridLayout;
	private Label StatusLabel;
	private CancellationTokenSource _searchCts;

	/// <summary>
	/// Called when the user wants to fork a game. Provides the selected Package.
	/// </summary>
	public Action<Package> OnForkRequested { get; set; }

	/// <summary>
	/// Called when the user clicks the back button to return to the project list.
	/// </summary>
	public Action OnBackRequested { get; set; }

	public GameBrowserWidget( Widget parent = null ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.Spacing = 4;

		// Top menu bar
		{
			var menuRow = Layout.AddRow();
			menuRow.Spacing = 4;
			menuRow.Margin = new Sandbox.UI.Margin( 16, 16, 16, 0 );

			var backButton = menuRow.Add( new IconButton( "arrow_back" ) { OnClick = () => OnBackRequested?.Invoke(), ToolTip = "Back to My Projects" } );

			var search = menuRow.Add( new LineEdit() { PlaceholderText = "\u2315  Search games..." }, 2 );
			search.SetStyles( "border-radius: 3px;" );
			search.TextChanged += ( text ) =>
			{
				_searchText = search.Value;
				search.Focus();
				_ = RefreshGamesAsync();
			};
			search.Blur();

			menuRow.AddStretchCell( 1 );
		}

		Layout.AddSpacingCell( 8.0f );

		// Status / loading label
		{
			StatusLabel = Layout.Add( new Label( "Loading games..." ) { ContentMargins = new Sandbox.UI.Margin( 24, 0, 0, 0 ) } );
		}

		// Scrollable game grid
		{
			var scroller = Layout.Add( new ScrollArea( this ), 1 );
			scroller.Canvas = new Widget( scroller )
			{
				Layout = Layout.Column(),
				VerticalSizeMode = SizeMode.CanGrow | SizeMode.Expand
			};

			GameGridLayout = scroller.Canvas.Layout.Add( Layout.Column() );
			scroller.Canvas.Layout.AddStretchCell();

			scroller.Canvas.OnPaintOverride = () =>
			{
				Paint.ClearPen();
				Paint.SetBrush( Theme.WindowBackground );
				Paint.DrawRect( scroller.Canvas.LocalRect );
				return true;
			};
		}

		_ = RefreshGamesAsync();
	}

	async Task RefreshGamesAsync()
	{
		// Cancel any previous search
		_searchCts?.Cancel();
		_searchCts = new CancellationTokenSource();
		var token = _searchCts.Token;

		StatusLabel.Text = "Searching...";
		StatusLabel.Visible = true;

		try
		{
			var query = $"type:game sort:popular";
			if ( !string.IsNullOrWhiteSpace( _searchText ) )
				query = $"type:game {_searchText}";

			var result = await Package.FindAsync( query, 50, 0, token );

			if ( token.IsCancellationRequested )
				return;

			var games = (result?.Packages ?? Array.Empty<Package>())
				.Where( x => x is not null && !x.Archived )
				.ToArray();

			// Dispatch UI updates to the main thread (SyncContext is not
			// initialized in the launcher, so MainThread.Queue is the
			// only safe way to touch widgets from an async continuation).
			MainThread.Queue( () =>
			{
				if ( token.IsCancellationRequested )
					return;

				if ( games.Length == 0 )
				{
					StatusLabel.Text = "No games found.";
					StatusLabel.Visible = true;
					GameGridLayout.Clear( true );
					return;
				}

				StatusLabel.Visible = false;
				UpdateGameGrid( games );
			} );
		}
		catch ( OperationCanceledException )
		{
			// Search was cancelled, ignore
		}
		catch ( Exception ex )
		{
			Log.Error( ex, $"Failed to refresh games" );
			MainThread.Queue( () =>
			{
				StatusLabel.Text = $"Error: {ex.Message}";
				StatusLabel.Visible = true;
			} );
		}
	}

	void UpdateGameGrid( Package[] games )
	{
		using var suspend = SuspendUpdates.For( this );

		GameGridLayout.Clear( true );
		GameGridLayout.Margin = new Sandbox.UI.Margin( 16, 0, 16, 16 );

		var grid = new GridLayout();
		grid.Spacing = 1;
		grid.HorizontalSpacing = 16;
		GameGridLayout.Add( grid );

		for ( int i = 0; i < games.Length; i++ )
		{
			var game = games[i];
			var row = new GameBrowserRow( game, this );
			row.Click = () => OnForkRequested?.Invoke( game );
			grid.AddCell( 0, i, row );
		}
	}

	protected override void OnPaint()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.WindowBackground );
		Paint.DrawRect( LocalRect );
	}
}

/// <summary>
/// A single row in the game browser grid, showing a game package with thumbnail, title, and info.
/// </summary>
public class GameBrowserRow : ItemRow
{
	Package Game { get; }

	public GameBrowserRow( Package game, Widget parent ) : base( parent )
	{
		Game = game;
		Title = game.Title ?? game.Ident;
		Init();
	}

	protected override List<InfoItem> GetInfo()
	{
		var info = new List<InfoItem>();

		if ( !string.IsNullOrEmpty( Game.Org?.Title ) )
			info.Add( ("group", Game.Org.Title) );

		if ( !string.IsNullOrEmpty( Game.Summary ) )
			info.Add( ("description", Game.Summary) );

		return info;
	}

	protected override void OnPaintIcon( Rect iconRect )
	{
		bool hasThumb = !string.IsNullOrEmpty( Game.Thumb ) && Game.Thumb.StartsWith( "http" );

		if ( hasThumb )
		{
			Paint.Draw( iconRect, Game.Thumb, borderRadius: 4 );
		}
		else
		{
			Paint.SetBrushAndPen( Theme.SurfaceBackground );
			Paint.DrawRect( iconRect, 4 );
			Paint.Pen = Theme.Text;
			Paint.DrawIcon( iconRect, "sports_esports", iconRect.Height * 0.6f );
		}
	}
}
