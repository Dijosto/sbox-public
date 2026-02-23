using Sandbox;
using Sandbox.DataModel;
using System.IO;

namespace Editor;

/// <summary>
/// Dialog for configuring a fork of a cloud game into a local project.
/// Shows the source game info and lets the user pick title, ident, and location.
/// </summary>
public class GameForkDialog : Dialog
{
	/// <summary>
	/// Invoked with the .sbproj path after a successful fork.
	/// </summary>
	public Action<string> OnProjectCreated { get; set; }

	Package SourceGame { get; }

	Button ForkButton;
	LineEdit TitleEdit;
	LineEdit IdentEdit;
	Checkbox CreateGitIgnore;
	FolderEdit FolderEdit;
	FieldSubtitle FolderFullPath;
	ErrorBox FolderError;
	Label ProgressLabel;

	bool identEdited;
	bool isBusy;

	public GameForkDialog( Package sourceGame, Widget parent = null ) : base( null )
	{
		SourceGame = sourceGame;

		Window.Size = new Vector2( 800, 500 );
		Window.MaximumSize = Window.Size;
		Window.MinimumSize = Window.Size;
		Window.SetModal( true, true );
		Window.Title = $"Fork Game - {sourceGame.Title}";
		Window.SetWindowIcon( "fork_right" );

		Layout = Layout.Row();
		Layout.Margin = 4;

		// Left pane — source game info
		{
			var column = Layout.AddColumn( 3 );
			column.Margin = 12;

			column.AddSpacingCell( 8.0f );
			column.Add( new Label.Subtitle( "Source Game" ) );
			column.AddSpacingCell( 18.0f );

			// Game thumbnail
			var thumbWidget = column.Add( new GameThumbWidget( sourceGame ) );
			column.AddSpacingCell( 12.0f );

			// Game title
			column.Add( new FieldTitle( sourceGame.Title ?? sourceGame.Ident ) );
			column.AddSpacingCell( 4.0f );

			// Organization
			if ( sourceGame.Org != null )
			{
				column.Add( new FieldSubtitle( $"by {sourceGame.Org.Title}" ) );
				column.AddSpacingCell( 8.0f );
			}

			// Summary
			if ( !string.IsNullOrEmpty( sourceGame.Summary ) )
			{
				var summary = column.Add( new FieldSubtitle( sourceGame.Summary ) );
				summary.WordWrap = true;
			}

			column.AddStretchCell( 1 );
		}

		// Right pane — project setup
		{
			var body = Layout.AddColumn( 2 );
			body.Margin = 12;
			body.Spacing = 8;

			body.AddSpacingCell( 8.0f );
			body.Add( new Label.Subtitle( "Project Setup" ) );
			body.AddSpacingCell( 12.0f );

			body.Add( new FieldTitle( "Title" ) );
			TitleEdit = body.Add( new LineEdit( "" ) { PlaceholderText = "My Forked Game" } );
			TitleEdit.Text = $"{sourceGame.Title ?? sourceGame.Ident} Fork";
			TitleEdit.TextEdited += ( x ) => Validate();

			body.AddSpacingCell( 8 );

			body.Add( new FieldTitle( "Ident" ) );
			body.Add( new FieldSubtitle( "Lowercase version of project name, no special characters" ) );
			IdentEdit = body.Add( new LineEdit( "" ) { PlaceholderText = "my_forked_game" } );
			IdentEdit.TextEdited += ( x ) => Validate();
			IdentEdit.TextEdited += ( x ) => identEdited = true;
			IdentEdit.SetValidator( "[a-z0-9_]{2,32}" );

			body.AddSpacingCell( 8 );

			body.Add( new FieldTitle( "Location" ) );
			FolderEdit = body.Add( new FolderEdit( null ) );
			FolderEdit.PlaceholderText = LauncherPreferences.DefaultProjectLocation.NormalizeFilename( false );
			FolderEdit.Text = LauncherPreferences.DefaultProjectLocation.NormalizeFilename( false );
			FolderEdit.TextEdited += ( x ) => Validate();
			FolderEdit.FolderSelected += ( x ) => Validate();

			FolderError = body.Add( new ErrorBox() );
			FolderError.Visible = false;
			FolderError.MinimumHeight = 34;
			FolderError.WordWrap = true;

			body.AddSpacingCell( 8 );

			body.Add( new FieldTitle( "Other" ) );
			CreateGitIgnore = body.Add( new Checkbox() );
			CreateGitIgnore.Value = true;
			CreateGitIgnore.Text = "Create .gitignore";

			body.AddStretchCell( 1 );

			// Progress label (shown during fork)
			ProgressLabel = body.Add( new Label( "" ) );
			ProgressLabel.Visible = false;

			var footer = body.AddRow();
			footer.Spacing = 8;

			FolderFullPath = footer.Add( new FieldSubtitle( "" ) );
			footer.AddStretchCell();

			ForkButton = footer.Add( new Button.Primary( "Fork", "fork_right" ) { Clicked = StartFork } );
		}

		Validate();
	}

	static string ConvertToIdent( string title )
	{
		return System.Text.RegularExpressions.Regex.Replace( title.ToLower(), "[^A-Za-z0-9_]", "_" ).Trim( '_' );
	}

	void Validate()
	{
		if ( isBusy ) return;

		if ( !identEdited )
		{
			IdentEdit.Text = ConvertToIdent( TitleEdit.Text );
		}

		bool enabled = true;
		if ( string.IsNullOrWhiteSpace( FolderEdit.Text ) ) enabled = false;
		if ( string.IsNullOrWhiteSpace( TitleEdit.Text ) ) enabled = false;
		if ( string.IsNullOrWhiteSpace( IdentEdit.Text ) ) enabled = false;

		FolderError.Visible = false;
		string fullPath = Path.Combine( FolderEdit.Text, IdentEdit.Text );
		FolderFullPath.Text = fullPath.NormalizeFilename( false );

		if ( Path.Exists( fullPath ) )
		{
			FolderError.Text = $"{FolderFullPath.Text} already exists";
			FolderError.Visible = true;
			enabled = false;
		}

		if ( IdentEdit.Text.Length >= 32 )
			IdentEdit.Text = IdentEdit.Text[..Math.Min( IdentEdit.Text.Length, 32 )];

		ForkButton.Enabled = enabled;
	}

	async void StartFork()
	{
		if ( isBusy ) return;

		isBusy = true;
		ForkButton.Enabled = false;
		ProgressLabel.Visible = true;
		ProgressLabel.Text = "Creating project...";

		try
		{
			var projectDir = Path.Combine( FolderEdit.Text, IdentEdit.Text );

			// Creates the .sbproj and directory structure. Code extraction and asset
			// loading happen on first editor open via StartupLoadProject.
			var configPath = await GameForker.CreateProject(
				SourceGame.FullIdent,
				projectDir,
				TitleEdit.Text,
				IdentEdit.Text
			);

			// Copy .gitignore if requested
			if ( CreateGitIgnore.Value )
			{
				var gitignorePath = Path.Combine( projectDir, ".gitignore" );
				if ( !File.Exists( gitignorePath ) )
				{
					var templatePath = FileSystem.Root.GetFullPath( "/templates/template.gitignore" );
					if ( File.Exists( templatePath ) )
						File.Copy( templatePath, gitignorePath );
				}
			}

			ProgressLabel.Text = "Fork complete!";

			Close();
			OnProjectCreated?.Invoke( configPath );
		}
		catch ( Exception ex )
		{
			ProgressLabel.Text = $"Error: {ex.Message}";
			isBusy = false;
			ForkButton.Enabled = true;
		}
	}
}

/// <summary>
/// Displays a game package thumbnail.
/// </summary>
internal class GameThumbWidget : Widget
{
	Package Game { get; }

	public GameThumbWidget( Package game, Widget parent = null ) : base( parent )
	{
		Game = game;
		FixedHeight = 120;
	}

	protected override void OnPaint()
	{
		var r = LocalRect.Shrink( 8, 0 );
		r.Height = 120;

		bool hasThumb = !string.IsNullOrEmpty( Game.Thumb ) && Game.Thumb.StartsWith( "http" );

		if ( hasThumb )
		{
			Paint.Draw( r, Game.Thumb, borderRadius: 8 );
		}
		else
		{
			Paint.SetBrushAndPen( Theme.SurfaceBackground );
			Paint.DrawRect( r, 8 );
			Paint.Pen = Theme.Text;
			Paint.DrawIcon( r, "sports_esports", 48 );
		}
	}
}
