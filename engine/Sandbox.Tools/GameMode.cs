using Native;

namespace Editor;

/// <summary>
/// Registers a widget with the input system, so it uses SDL.
/// </summary>
public static class GameMode
{
	static Widget _inPlay;

	/// <summary>
	/// Given a widget, register it for SDL input, and tell the engine this is the swapchain we have
	/// </summary>
	/// <param name="widget"></param>
	public static void SetPlayWidget( SceneRenderingWidget widget )
	{
		if ( _inPlay == widget ) return;

		widget.Focused += WidgetFocused;
		widget.Blurred += WidgetBlurred;

		NativeEngine.InputSystem.RegisterWindowWithSDL( widget._widget.winId() );
		g_pEngineServiceMgr.SetEngineState( widget._widget.winId(), widget.SwapChain );

		_inPlay = widget;

		// Force a full refocus by blurring first
		widget.Blur();
		widget.Focus();
	}

	public static void ClearPlayMode()
	{
		if ( _inPlay is null )
			return;

		_inPlay.Blur();

		_inPlay.Focused -= WidgetFocused;
		_inPlay.Blurred -= WidgetBlurred;

		NativeEngine.InputSystem.UnregisterWindowFromSDL( _inPlay._widget.winId() );

		_inPlay = null;
	}

	/// <summary>
	/// When the editor gains focus of the game widget, tell the input system so it'll mouse capture (if it wants to)
	/// </summary>
	private static void WidgetFocused( FocusChangeReason reason )
	{
		NativeEngine.InputSystem.OnEditorGameFocusChange( _inPlay._widget.winId(), true );
	}

	/// <summary>
	/// When the editor loses focus of the game widget, tell the input system so it stops trying to do mouse capture.
	/// </summary>
	private static void WidgetBlurred( FocusChangeReason reason )
	{
		NativeEngine.InputSystem.OnEditorGameFocusChange( _inPlay._widget.winId(), false );
	}
}
