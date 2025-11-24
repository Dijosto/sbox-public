using Sandbox.Rendering;

namespace Sandbox.UI
{

	internal partial class PanelRenderer
	{
		private void SetColor( RenderAttributes attr, string v, Color color, float opacity )
		{
			if ( opacity < 1 )
			{
				color.a *= opacity;
			}

			attr.Set( v, color );
		}

		void SetBorderRadius( Styles style, float size )
		{
			var borderRadius = new Vector4(
				style.BorderBottomRightRadius.Value.GetPixels( size ),
				style.BorderTopRightRadius.Value.GetPixels( size ),
				style.BorderBottomLeftRadius.Value.GetPixels( size ),
				style.BorderTopLeftRadius.Value.GetPixels( size )
			);
			Graphics.Attributes.Set( "BorderRadius", borderRadius );
		}

		internal void DrawBackgroundTexture( Panel panel, Texture texture, in RenderState state, Length defaultSize )
		{
			var style = panel.ComputedStyle;
			if ( style == null ) return; // Not layed out yet

			var attr = RenderAttributes.Pool.Get();

			if ( texture == Texture.Invalid )
				texture = null;

			var rect = panel.Box.Rect;
			var opacity = panel.Opacity * state.RenderOpacity;

			var color = style.BackgroundColor.Value;
			color.a *= opacity;

			var size = (rect.Width + rect.Height) * 0.5f;

			var borderSize = new Vector4(
				style.BorderLeftWidth.Value.GetPixels( size ),
				style.BorderTopWidth.Value.GetPixels( size ),
				style.BorderRightWidth.Value.GetPixels( size ),
				style.BorderBottomWidth.Value.GetPixels( size )
			);

			attr.Set( "BoxPosition", new Vector2( rect.Left, rect.Top ) );
			attr.Set( "BoxSize", new Vector2( rect.Width, rect.Height ) );

			SetBorderRadius( style, size );

			if ( borderSize.x == 0 && borderSize.y == 0 && borderSize.z == 0 && borderSize.w == 0 )
			{
				attr.Set( "HasBorder", 0 );
			}
			else
			{
				attr.Set( "HasBorder", 1 );
				attr.Set( "BorderSize", borderSize );

				SetColor( attr, "BorderColorL", style.BorderLeftColor.Value, opacity );
				SetColor( attr, "BorderColorT", style.BorderTopColor.Value, opacity );
				SetColor( attr, "BorderColorR", style.BorderRightColor.Value, opacity );
				SetColor( attr, "BorderColorB", style.BorderBottomColor.Value, opacity );
			}

			// We have a border image
			if ( style.BorderImageSource != null )
			{
				attr.Set( "BorderImageTexture", style.BorderImageSource );
				attr.Set( "BorderImageSlice", new Vector4(
					style.BorderImageWidthLeft.Value.GetPixels( size ),
					style.BorderImageWidthTop.Value.GetPixels( size ),
					style.BorderImageWidthRight.Value.GetPixels( size ),
					style.BorderImageWidthBottom.Value.GetPixels( size ) )
				);
				attr.SetCombo( "D_BORDER_IMAGE", (byte)(style.BorderImageRepeat == BorderImageRepeat.Stretch ? 2 : 1) );
				attr.Set( "HasBorderImageFill", (byte)(style.BorderImageFill == BorderImageFill.Filled ? 1 : 0) );

				SetColor( attr, "BorderImageTint", style.BorderImageTint.Value, opacity );
			}
			else
			{
				attr.SetCombo( "D_BORDER_IMAGE", 0 );
			}

			var backgroundRepeat = style.BackgroundRepeat ?? BackgroundRepeat.Repeat;

			if ( texture != null )
			{
				var imageRectInput = new ImageRect.Input
				{
					ScaleToScreen = panel.ScaleToScreen,
					Image = texture,
					PanelRect = rect,
					DefaultSize = defaultSize,
					ImagePositionX = style.BackgroundPositionX,
					ImagePositionY = style.BackgroundPositionY,
					ImageSizeX = style.BackgroundSizeX,
					ImageSizeY = style.BackgroundSizeY,
				};

				var imageCalc = ImageRect.Calculate( imageRectInput );

				attr.Set( "Texture", texture );
				attr.Set( "BgPos", imageCalc.Rect );
				attr.Set( "BgAngle", style.BackgroundAngle.Value.GetPixels( 1.0f ) );
				attr.Set( "BgRepeat", (int)backgroundRepeat );

				attr.SetCombo( "D_BACKGROUND_IMAGE", 1 );

				SetColor( attr, "BgTint", style.BackgroundTint.Value, opacity );
			}
			else
			{
				attr.SetCombo( "D_BACKGROUND_IMAGE", 0 );
			}

			var filter = (style?.ImageRendering ?? ImageRendering.Anisotropic) switch
			{
				ImageRendering.Point => FilterMode.Point,
				ImageRendering.Bilinear => FilterMode.Bilinear,
				ImageRendering.Trilinear => FilterMode.Trilinear,
				_ => FilterMode.Anisotropic
			};

			var sampler = backgroundRepeat switch
			{
				BackgroundRepeat.RepeatX => new SamplerState { AddressModeV = TextureAddressMode.Clamp, Filter = filter },
				BackgroundRepeat.RepeatY => new SamplerState { AddressModeU = TextureAddressMode.Clamp, Filter = filter },
				BackgroundRepeat.Clamp => new SamplerState
				{
					AddressModeU = TextureAddressMode.Clamp,
					AddressModeV = TextureAddressMode.Clamp,
					Filter = filter
				},
				_ => new SamplerState { Filter = filter }
			};

			attr.Set( "SamplerIndex", SamplerState.GetBindlessIndex( sampler ) );
			attr.Set( "ClampSamplerIndex", SamplerState.GetBindlessIndex( new SamplerState
			{
				AddressModeU = TextureAddressMode.Clamp,
				AddressModeV = TextureAddressMode.Clamp,
				Filter = filter
			} ) );

			attr.SetComboEnum( "D_BLENDMODE", OverrideBlendMode );

			Graphics.DrawQuad( rect, Material.UI.Box, color, attr );

			RenderAttributes.Pool.Return( attr );
		}

		RenderAttributes shadowAttr = new RenderAttributes();

		public void DrawBoxShadow( Panel panel, ref RenderState state, in Shadow shadow )
		{
			if ( shadow.Color.a <= 0 ) return;

			var inset = shadow.Inset;
			var style = panel.ComputedStyle;
			var rect = panel.Box.Rect;
			var size = (rect.Width + rect.Height) * 0.5f;
			var shadowOffset = new Vector2( shadow.OffsetX, shadow.OffsetY );
			var shadowrect = inset ? rect : rect + shadowOffset;

			var blur = shadow.Blur;
			var spread = shadow.Spread;
			var borderRadius = new Vector4(
				style.BorderTopLeftRadius.Value.GetPixels( size ),
				style.BorderTopRightRadius.Value.GetPixels( size ),
				style.BorderBottomLeftRadius.Value.GetPixels( size ),
				style.BorderBottomRightRadius.Value.GetPixels( size )
			);

			shadowrect = shadowrect.Grow( spread );

			var opacity = panel.Opacity * state.RenderOpacity;
			var color = shadow.Color;
			color.a *= opacity;

			shadowAttr.Clear();
			shadowAttr.Set( "BoxPosition", new Vector2( shadowrect.Left, shadowrect.Top ) );
			shadowAttr.Set( "BoxSize", new Vector2( shadowrect.Width, shadowrect.Height ) );
			shadowAttr.Set( "BorderRadius", borderRadius );
			shadowAttr.Set( "ShadowWidth", blur );
			shadowAttr.Set( "ShadowOffset", shadowOffset );
			shadowAttr.Set( "Bloat", blur );
			shadowAttr.Set( "Inset", inset );
			shadowAttr.SetComboEnum( "D_BLENDMODE", OverrideBlendMode );

			if ( inset )
			{
				// Inset shadows appear inside the panel, so we clip for that
				shadowAttr.Set( "ScissorRect", panel.Box.ClipRect.ToVector4() );
				shadowAttr.Set( "ScissorCornerRadius", borderRadius );
				shadowAttr.Set( "ScissorTransformMat", panel.GlobalMatrix ?? Matrix.Identity );
				shadowAttr.Set( "HasScissor", 1 );
			}
			else
			{
				// Normal/outset shadows appear outside the panel
				shadowAttr.Set( "InverseScissorRect", panel.Box.ClipRect.ToVector4() );
				shadowAttr.Set( "InverseScissorCornerRadius", borderRadius );
				shadowAttr.Set( "InverseScissorTransformMat", panel.GlobalMatrix ?? Matrix.Identity );
				shadowAttr.Set( "HasInverseScissor", 1 );
			}

			Graphics.DrawQuad( shadowrect.Grow( blur ), Material.UI.BoxShadow, color, shadowAttr );
		}

		/// <summary>
		/// Draw the outset box shadows - this is called *before* drawing the background
		/// </summary>
		public void DrawBoxShadows( Panel panel, ref RenderState state, bool inset )
		{
			var shadows = panel.ComputedStyle.BoxShadow;
			var c = shadows.Count;

			if ( c == 0 )
				return;

			for ( int i = 0; i < c; i++ )
			{
				if ( shadows[i].Inset != inset ) continue;
				DrawBoxShadow( panel, ref state, shadows[i] );
			}
		}

		public void DrawRect( Rect rect, Texture texture, Color color )
		{
			var attr = RenderAttributes.Pool.Get();
			attr.Set( "BoxPosition", new Vector2( rect.Left, rect.Top ) );
			attr.Set( "BoxSize", new Vector2( rect.Width, rect.Height ) );
			attr.Set( "BorderRadius", new Vector4() );
			attr.Set( "BorderSize", new Vector4() );
			attr.Set( "Texture", texture );
			attr.SetCombo( "D_BACKGROUND_IMAGE", 1 );
			attr.Set( "BgPos", new Vector4( 0, 0, rect.Width, rect.Height ) );
			attr.SetComboEnum( "D_BLENDMODE", OverrideBlendMode );

			Graphics.DrawQuad( rect, Material.UI.Box, color, attr );

			RenderAttributes.Pool.Return( attr );
		}
	}
}
