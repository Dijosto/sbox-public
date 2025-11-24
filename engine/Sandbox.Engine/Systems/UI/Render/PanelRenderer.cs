namespace Sandbox.UI;

internal unsafe sealed partial class PanelRenderer
{
	[ConVar( ConVarFlags.Protected, Help = "Enable drawing text" )]
	public static bool ui_drawtext { get; set; } = true;

	public Rect Screen { get; internal set; }

	public void Render( RootPanel panel, float opacity = 1.0f )
	{
		ThreadSafe.AssertIsMainThread();

		Screen = panel.PanelBounds;

		MatrixStack.Clear();
		MatrixStack.Push( Matrix.Identity );
		Matrix = Matrix.Identity;

		RenderModeStack.Clear();
		RenderModeStack.Push( "normal" );
		RenderMode = null;
		SetRenderMode( "normal" );

		LayerStack?.Clear();
		Graphics.Attributes.Set( "LayerMat", Matrix.Identity );
		DirtyFramebufferRegion.Clear();
		InitScissor( Screen );
		Render( panel, new RenderState { X = Screen.Left, Y = Screen.Top, Width = Screen.Width, Height = Screen.Height, RenderOpacity = opacity } );
	}

	/// <summary>
	/// Render a panel
	/// </summary>
	public void Render( Panel panel, RenderState state )
	{
		if ( panel?.ComputedStyle == null )
			return;

		if ( !panel.IsVisible )
			return;

		//
		// Save off render target - mainly for VR, if we're using a VROverlayPanel
		// we want to default to using the render texture for that
		//
		var defaultRenderTarget = Graphics.RenderTarget;

		//
		// Push matrix before culling so Panel.GlobalMatrix is set
		//
		var pushed = PushMatrix( panel );

		//
		// Quickly clip anything before sending to renderer, this doesn't need to be perfect
		//
		if ( ShouldEarlyCull( panel ) )
		{
			if ( pushed ) PopMatrix();
			return;
		}

		var renderMode = PushRenderMode( panel );

		{
			DrawBoxShadows( panel, ref state, false );

			panel.PushLayer( this );
			panel.DrawBackground( this, ref state );

			DrawBoxShadows( panel, ref state, true );

			//
			// Content = Text, Image (not children)
			//
			if ( panel.HasContent )
			{
				try
				{
					panel.DrawContent( this, ref state );
				}
				catch ( System.Exception e )
				{
					Log.Error( e );
				}
			}
		}

		if ( panel.HasChildren )
		{
			panel.RenderChildren( this, ref state );
		}

		panel.PopLayer( this, defaultRenderTarget );

		if ( pushed ) PopMatrix();
		if ( renderMode ) PopRenderMode();
	}

	struct LayerEntry
	{
		public Texture Texture;
		public Matrix Matrix;
	}

	Stack<LayerEntry> LayerStack;

	internal bool IsWorldPanel( Panel panel )
	{
		if ( panel is RootPanel { IsWorldPanel: true } )
			return true;

		if ( panel.FindRootPanel()?.IsWorldPanel ?? false )
			return true;

		return false;
	}

	internal void PushLayer( Panel panel, Texture texture, Matrix mat )
	{
		LayerStack ??= new Stack<LayerEntry>();

		bool isWorldPanel = (panel is RootPanel { IsWorldPanel: true });

		Graphics.RenderTarget = RenderTarget.From( texture );
		Graphics.Attributes.Set( "LayerMat", mat );
		Graphics.Attributes.SetCombo( "D_WORLDPANEL", 0 );

		Graphics.Clear();

		LayerStack.Push( new LayerEntry { Texture = texture, Matrix = mat } );
	}

	/// <returns>Will return <c>false</c> if we're at the top of the layer stack.</returns>
	internal void PopLayer( Panel panel, RenderTarget defaultRenderTarget )
	{
		DirtyFramebufferRegion.Clear();
		LayerStack.Pop();

		bool isWorldPanel = (panel is RootPanel { IsWorldPanel: true });

		if ( LayerStack.TryPeek( out var top ) )
		{
			Graphics.RenderTarget = RenderTarget.From( top.Texture );
			Graphics.Attributes.Set( "LayerMat", top.Matrix );

			Graphics.Attributes.SetCombo( "D_WORLDPANEL", 0 );
		}
		else
		{
			Graphics.RenderTarget = defaultRenderTarget;
			Graphics.Attributes.Set( "LayerMat", Matrix.Identity );

			Graphics.Attributes.SetCombo( "D_WORLDPANEL", IsWorldPanel( panel ) );
		}
	}
}
