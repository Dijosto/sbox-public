using Sandbox.Rendering;

namespace Sandbox.UI;

internal class PanelLayer : IDisposable
{
	public Vector2 Size { get; set; }
	public Texture Texture { get; set; }

	public PanelLayer( Vector2 size )
	{
		Size = size;
		Texture = Texture.CreateRenderTarget()
								.WithSize( Size )
								.Create();
	}

	public void Dispose()
	{
		Texture?.Dispose();
		Texture = null;
	}
}

public partial class Panel
{
	PanelLayer PanelLayer;

	bool NeedsLayer( Styles styles )
	{
		if ( HasFilter ) return true;
		if ( styles.FilterDropShadow.Count > 0 ) return true;
		if ( styles.MaskImage != null ) return true;

		//
		// While this works, it seems really pretty wasteful
		//
		//if ( styles.Overflow.HasValue && styles.Overflow != OverflowMode.Visible ) return true;

		return false;
	}

	void UpdateLayer( Styles styles )
	{
		if ( NeedsLayer( styles ) )
		{
			var size = Box.RectOuter.Size;

			//	if ( styles.Overflow.HasValue && styles.Overflow != OverflowMode.Visible )
			//	{
			//		size = Box.Rect.Size;
			//	}

			if ( size.x <= 1 ) return;
			if ( size.y <= 1 ) return;

			// TODO - add blur size margin
			if ( PanelLayer != null && PanelLayer.Size == size )
				return;

			PanelLayer?.Dispose();
			PanelLayer = null;

			PanelLayer = new PanelLayer( size );
		}
		else
		{
			PanelLayer?.Dispose();
			PanelLayer = null;
		}
	}

	/// <summary>
	/// Called before rendering this panel
	/// </summary>
	internal void PushLayer( PanelRenderer render )
	{
		if ( PanelLayer == null ) return;
		if ( ComputedStyle == null ) return;
		if ( !IsVisible ) return;

		// we need to push a matrix to offset the panel position,
		// so we're drawing in the correct place
		var mat = render.Matrix.Inverted;
		mat *= Matrix.CreateTranslation( Box.RectOuter.Position * -1.0f );

		render.PushLayer( this, PanelLayer.Texture, mat );
	}

	RenderAttributes layerAttributes = new RenderAttributes();

	/// <summary>
	/// Called after rendering this panel
	/// </summary>
	internal void PopLayer( PanelRenderer render, RenderTarget defaultRenderTarget )
	{
		if ( PanelLayer == null ) return;
		if ( ComputedStyle == null ) return;
		if ( !IsVisible ) return;

		render.PopLayer( this, defaultRenderTarget );

		//
		// Shared attributes
		//
		layerAttributes.Set( "Texture", PanelLayer.Texture );
		layerAttributes.Set( "BoxPosition", Box.RectOuter.Position );
		layerAttributes.Set( "BoxSize", Box.RectOuter.Size );

		//
		// Pre-filter: draw shadows and border before everything else as separate layers
		//
		DrawPreFilterShadows();
		DrawPreFilterBorder();
		ResetPrefilterAttributes();

		//
		// Draw this panel, with filters
		//
		// Todo, awesome, smooth, multipass blur
		float blurSize = ComputedStyle.FilterBlur.Value.GetPixels( 1.0f );
		layerAttributes.Set( "FilterBlur", blurSize );

		layerAttributes.Set( "FilterSaturate", ComputedStyle.FilterSaturate.Value.GetFraction( 1.0f ) );
		layerAttributes.Set( "FilterSepia", ComputedStyle.FilterSepia.Value.GetFraction( 1.0f ) );
		layerAttributes.Set( "FilterBrightness", ComputedStyle.FilterBrightness.Value.GetPixels( 1.0f ) );
		layerAttributes.Set( "FilterContrast", ComputedStyle.FilterContrast.Value.GetPixels( 1.0f ) );
		layerAttributes.Set( "FilterInvert", ComputedStyle.FilterInvert.Value.GetPixels( 1.0f ) );
		layerAttributes.Set( "FilterHueRotate", ComputedStyle.FilterHueRotate.Value.GetPixels( 1.0f ) );
		layerAttributes.Set( "FilterTint", ComputedStyle.FilterTint ?? Vector4.One );

		float growSize = blurSize;

		//
		// Handle masks
		//
		bool hasMask = ComputedStyle.MaskImage != null;
		layerAttributes.SetCombo( "D_MASK_IMAGE", hasMask ? 1 : 0 );

		if ( hasMask )
		{
			var imageRectInput = new ImageRect.Input
			{
				ScaleToScreen = ScaleToScreen,
				Image = ComputedStyle?.MaskImage,
				PanelRect = Box.RectOuter,
				DefaultSize = Length.Auto,
				ImagePositionX = ComputedStyle.MaskPositionX,
				ImagePositionY = ComputedStyle.MaskPositionY,
				ImageSizeX = ComputedStyle.MaskSizeX,
				ImageSizeY = ComputedStyle.MaskSizeY,
			};

			var maskCalc = ImageRect.Calculate( imageRectInput );

			layerAttributes.Set( "MaskPos", maskCalc.Rect );
			layerAttributes.Set( "MaskTexture", ComputedStyle?.MaskImage );
			layerAttributes.Set( "MaskMode", (int)(ComputedStyle?.MaskMode ?? MaskMode.MatchSource) );
			layerAttributes.Set( "MaskAngle", ComputedStyle?.MaskAngle?.GetPixels( 1.0f ) ?? 0.0f );
			layerAttributes.Set( "MaskScope", (int)(ComputedStyle?.MaskScope ?? MaskScope.Default) );

			var filter = (ComputedStyle?.ImageRendering ?? ImageRendering.Anisotropic) switch
			{
				ImageRendering.Point => FilterMode.Point,
				ImageRendering.Bilinear => FilterMode.Bilinear,
				ImageRendering.Trilinear => FilterMode.Trilinear,
				_ => FilterMode.Anisotropic
			};

			var sampler = (ComputedStyle?.MaskRepeat ?? BackgroundRepeat.Repeat) switch
			{
				BackgroundRepeat.RepeatX => new SamplerState { AddressModeV = TextureAddressMode.Clamp, Filter = filter },
				BackgroundRepeat.RepeatY => new SamplerState { AddressModeU = TextureAddressMode.Clamp, Filter = filter },
				BackgroundRepeat.NoRepeat => new SamplerState
				{
					AddressModeU = TextureAddressMode.Border,
					AddressModeV = TextureAddressMode.Border,
					Filter = filter
				},
				BackgroundRepeat.Clamp => new SamplerState
				{
					AddressModeU = TextureAddressMode.Clamp,
					AddressModeV = TextureAddressMode.Clamp,
					Filter = filter
				},
				_ => new SamplerState { Filter = filter }
			};

			layerAttributes.Set( "SamplerIndex", SamplerState.GetBindlessIndex( sampler ) );
			layerAttributes.Set( "BorderSamplerIndex", SamplerState.GetBindlessIndex( new SamplerState
			{
				AddressModeU = TextureAddressMode.Border,
				AddressModeV = TextureAddressMode.Border,
				Filter = filter
			} ) );
		}

		layerAttributes.SetComboEnum( "D_BLENDMODE", render.OverrideBlendMode );
		Graphics.DrawQuad( Box.RectOuter.Grow( growSize ).Ceiling(), Material.UI.Filter, Color.White, layerAttributes );
	}

	/// <summary>
	/// Draws shadows for the current layer 
	/// </summary>
	private void DrawPreFilterShadows()
	{
		foreach ( var shadow in ComputedStyle.FilterDropShadow )
		{
			var outerRect = Box.RectOuter;

			var shadowSize = new Vector2( shadow.OffsetX, shadow.OffsetY );

			// Grow outerRect so that it can fit the shadow
			float growSize = MathF.Max( shadowSize.x, shadowSize.y );
			growSize = MathF.Max( 1.0f, growSize );
			growSize *= MathF.Max( 1.0f, shadow.Blur * 2.0f );
			outerRect = outerRect.Grow( growSize );

			ResetPrefilterAttributes();

			layerAttributes.Set( "FilterDropShadowScale", Box.RectOuter.Size / outerRect.Size );
			layerAttributes.Set( "FilterDropShadowOffset", shadowSize );
			layerAttributes.Set( "FilterDropShadowBlur", shadow.Blur );
			layerAttributes.Set( "FilterDropShadowColor", shadow.Color );

			Graphics.DrawQuad( outerRect, Material.UI.DropShadow, Color.White, layerAttributes );
		}
	}

	/// <summary>
	/// Draws borders for the current layer
	/// </summary>
	private void DrawPreFilterBorder()
	{
		float filterBorderWidth = ComputedStyle.FilterBorderWidth.Value.GetPixels( 1.0f );
		filterBorderWidth *= ScaleToScreen;

		if ( filterBorderWidth > 0.0f )
		{
			var outerRect = Box.RectOuter;

			// Grow outerRect so that it can fit the border
			outerRect = outerRect.Grow( filterBorderWidth );

			ResetPrefilterAttributes();

			layerAttributes.Set( "FilterBorderWrapColorScale", Box.RectOuter.Size / outerRect.Size );
			layerAttributes.Set( "FilterBorderWrapColor", ComputedStyle.FilterBorderColor.Value );
			layerAttributes.Set( "FilterBorderWrapWidth", filterBorderWidth );

			Graphics.DrawQuad( outerRect, Material.UI.BorderWrap, Color.White, layerAttributes );
		}
	}

	private void ResetPrefilterAttributes()
	{
		layerAttributes.Set( "FilterDropShadowScale", 0 );
		layerAttributes.Set( "FilterDropShadowOffset", 0 );
		layerAttributes.Set( "FilterDropShadowBlur", 0 );
		layerAttributes.Set( "FilterDropShadowColor", 0 );

		layerAttributes.Set( "FilterBorderWrapColor", 0 );
		layerAttributes.Set( "FilterBorderWrapWidth", 0 );
	}
}
