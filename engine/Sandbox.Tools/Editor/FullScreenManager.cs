namespace Editor;

/// <summary>
/// A class that handles the fullscreen behavior for the main editor window.
/// </summary>
internal partial class FullScreenManager
{
	public bool IsActive => Widget.IsValid();

	/// <summary>
	/// The current fullscreen widget
	/// </summary>
	public Widget Widget { get; private set; }

	/// <summary>
	/// A reference to the previous parent, when killing fullscreen we want to restore the widget 
	/// </summary>
	private Widget PreviousParent { get; set; }

	/// <summary>
	/// Removes the current widget as the full screen widget
	/// </summary>
	public void Clear()
	{
		if ( !Widget.IsValid() )
			return;

		if ( EditorWindow.Console.IsValid() )
		{
			EditorWindow.Console.Input.FocusMode = FocusMode.TabOrClick;
		}

		Widget.Parent = PreviousParent;

		if ( PreviousParent.IsValid() )
		{
			PreviousParent.Layout?.Add( Widget );
		}

		Widget = null;
		PreviousParent = null;

		EditorEvent.Register( this );
	}

	[EditorEvent.Frame]
	public void OnFrame()
	{
		if ( !Widget.IsValid() )
			return;

		if ( Widget.Size != GetTargetSize() || Widget.Position != GetTargetPosition() )
		{
			SetWidgetLayout();
		}
	}

	private Vector2 GetTargetPosition()
	{
		return new Vector2( 0, EditorWindow.MenuWidget.Size.y );
	}

	private Vector2 GetTargetSize()
	{
		return EditorWindow.Size - Widget.Position - new Vector2( 0, EditorWindow.StatusBar.Size.y + 4 );
	}

	private void SetWidgetLayout()
	{
		if ( !Widget.IsValid() )
			return;

		Widget.Position = GetTargetPosition();
		Widget.Size = GetTargetSize();
	}

	/// <summary>
	/// Sets a widget as the fullscreen widget
	/// </summary>
	/// <param name="widget"></param>
	public void SetWidget( Widget widget )
	{
		Clear();

		if ( !widget.IsValid() )
			return;

		// Store the widget, and the widget's parent so we can restore it later
		Widget = widget;
		PreviousParent = widget.Parent;

		// Set our target widget's parent to the editor's main window, so we can size it properly
		widget.Parent = EditorWindow;

		// Make sure we kill focus from the console
		if ( EditorWindow.Console.IsValid() )
		{
			EditorWindow.Console.Input.FocusMode = FocusMode.None;
			EditorWindow.Console.Input.Blur();
		}

		// Make sure it's visible now
		widget.Visible = true;

		// Lay the widget out
		SetWidgetLayout();

		// Maximized window has no margin - we need to account for that
		// Typical window margin is 8,8,8,8 so we'll use those values
		if ( EditorWindow.IsMaximized )
		{
			Widget.Position += new Vector2( 8, 8 );
			Widget.Size -= new Vector2( 8, 8 );
		}
	}
}
