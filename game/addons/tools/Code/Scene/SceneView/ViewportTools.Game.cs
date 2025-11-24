using Sandbox.Diagnostics;

namespace Editor;

partial class ViewportTools
{
	private Label FrameTimeLabel;
	private Label FrameRateLabel;
	private ResolutionModeButton ResolutionComboBox;

	private void BuildToolbarGame( Layout layout )
	{
		{
			var group = AddGroup();

			{
				ResolutionComboBox = new( sceneViewWidget )
				{
					ToolTip = "Force a specific screen resolution or aspect ratio",
					FixedHeight = 22
				};

				group.Layout.Add( ResolutionComboBox );
			}

			layout.Add( group );
		}

		AddSeparator( layout );

		{
			var group = AddGroup();

			FrameTimeLabel = group.Layout.Add( new Label( $"--- ms" ) );
			FrameTimeLabel.SetStyles( $"font-family: '{Theme.MonospaceFont}';" );
			FrameTimeLabel.Alignment = TextFlag.Center;
			FrameTimeLabel.FixedWidth = 50;

			layout.Add( group );
		}

		{
			var group = AddGroup();

			FrameRateLabel = group.Layout.Add( new Label( $"--- FPS" ) );
			FrameRateLabel.SetStyles( $"font-family: '{Theme.MonospaceFont}';" );
			FrameRateLabel.Alignment = TextFlag.Center;
			FrameRateLabel.FixedWidth = 50;

			layout.Add( group );
		}
	}

	[EditorEvent.Frame]
	public void OnFrame()
	{
		if ( sceneViewWidget.CurrentView != SceneViewWidget.ViewMode.Game )
			return;

		if ( FrameTimeLabel == null || FrameRateLabel == null || ResolutionComboBox == null )
			return;

		FrameTimeLabel.Text = $"{PerformanceStats.LastSecond.FrameAvg:0.00}ms";
		FrameRateLabel.Text = $"{(1000.0f / PerformanceStats.LastSecond.FrameAvg):0} FPS";
		ResolutionComboBox.Suffix = ResolutionComboBox.GetSizeString();
	}
}


internal class ResolutionModeButton : Button
{
	private int _selectedIndex = 0;
	private List<Option> _options;

	public string Suffix { get; set; } = "";

	private SceneViewWidget _sceneViewWidget;

	public ResolutionModeButton( SceneViewWidget sceneViewWidget ) : base( null )
	{
		SetStyles( $"padding-left: 32px; padding-right: 32px; font-family: '{Theme.DefaultFont}'; padding-top: 6px; padding-bottom: 6px;" );
		FixedWidth = 210;
		FixedHeight = Theme.RowHeight + 6;

		InitializeOptions();
		UpdateButtonText();

		Clicked = Click;

		_sceneViewWidget = sceneViewWidget;
	}

	private void InitializeOptions()
	{
		_options =
		[
			new Option( "Free Size", "fit_screen", () => SetFreeSize() ),
			new Option( "16:9 Aspect", "aspect_ratio", () => SetForceAspect( 16.0f / 9.0f ) ),
			new Option( "21:9 Aspect", "aspect_ratio", () => SetForceAspect( 21.0f / 9.0f ) ),
			new Option( "4:3 Aspect", "aspect_ratio", () => SetForceAspect( 4.0f / 3.0f ) ),
			new Option( "9:16 Aspect", "aspect_ratio", () => SetForceAspect( 9.0f / 16.0f ) ),
			new Option( "Steam Deck", "stay_current_landscape", () => SetForceResolution( new( 1280, 800 ) ) ),
			new Option( "720p", "laptop_windows", () => SetForceResolution( new( 1280, 720 ) ) ),
			new Option( "1080p", "desktop_windows", () => SetForceResolution( new( 1920, 1080 ) ) ),
		];
	}

	void SetForceAspect( float aspect )
	{
		var viewport = _sceneViewWidget.GetGameTarget();
		// viewport.SetForceAspect( aspect );

		UpdateButtonText();
	}

	void SetFreeSize()
	{
		var viewport = _sceneViewWidget.GetGameTarget();
		// viewport.SetFreeSize();

		UpdateButtonText();
	}

	void SetForceResolution( Vector2 res )
	{
		var viewport = _sceneViewWidget.GetGameTarget();
		// viewport.SetForceResolution( res );

		UpdateButtonText();
	}

	public string GetSizeString()
	{
		var viewport = _sceneViewWidget.GetGameTarget();

		return $"{viewport.Renderer.Width}x{viewport.Renderer.Height}";
	}

	private void UpdateButtonText()
	{
		if ( _selectedIndex > _options.Count )
			return;

		if ( _selectedIndex < 0 )
			return;

		Text = _options[_selectedIndex].Text;
		Icon = _options[_selectedIndex].Icon;
	}

	private void Click()
	{
		var menu = new ContextMenu();

		for ( int i = 0; i < _options.Count; i++ )
		{
			Option option = _options[i];
			var index = i;

			menu.AddOption( option.Text, option.Icon, () =>
			{
				_selectedIndex = index;
				option.Triggered?.Invoke();
			} );
		}

		menu.OpenAt( ScreenRect.BottomLeft, false );
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		UpdateButtonText();
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( LocalRect, Theme.ControlRadius );

		var fg = Theme.Text;

		Paint.SetDefaultFont();
		Paint.SetPen( fg.WithAlphaMultiplied( Paint.HasMouseOver ? 1.0f : 0.9f ) );
		Paint.DrawIcon( LocalRect.Shrink( 8, 0, 0, 0 ), Icon, 14, TextFlag.LeftCenter );
		Paint.DrawText( LocalRect.Shrink( 32, 0, 0, 0 ), $"{Text}", TextFlag.LeftCenter );

		Paint.DrawIcon( LocalRect.Shrink( 4, 0 ), "arrow_drop_down", 18, TextFlag.RightCenter );

		Paint.SetPen( Theme.TextLight.WithAlphaMultiplied( Paint.HasMouseOver ? 1.0f : 0.9f ) );
		Paint.DrawText( LocalRect.Shrink( 32, 0, 32, 0 ), $"{Suffix}", TextFlag.RightCenter );
	}
}
