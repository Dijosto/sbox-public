namespace Sandbox.UI
{

	internal partial class PanelRenderer
	{
		internal List<Rect> DirtyFramebufferRegion = new();

		private void CopyFrameBufferCached( Rect rect )
		{
			rect = rect.Shrink( 1.0f ); // Shrink by 1 pixel to avoid sampling without real intersection

			bool isDirty = false;
			if ( DirtyFramebufferRegion.Count > 0 )
			{
				// Lets see if our region overlaps with any already used regions
				foreach ( var frame in DirtyFramebufferRegion )
				{
					// Check if anything overlaps
					if ( rect.IsInside( frame ) )
					{
						// We're overlapping on something which coppied the last framebuffer, need update!
						isDirty = true;
						break;
					}
				}

				if ( !isDirty )
				{
					// We can still use the last frame buffer, lets continue
					DirtyFramebufferRegion.Add( rect );
				}
			}
			else
			{
				isDirty = true;
			}

			if ( isDirty )
			{
				DirtyFramebufferRegion.Clear();
				// Add to our dirty list
				Graphics.GrabFrameTexture( "FrameBufferCopyTexture", renderAttributes: null, downsampleMethod: Graphics.DownsampleMethod.GaussianBlur );
				DirtyFramebufferRegion.Add( rect );
			}
		}

		internal void DrawBackdropFilters( Panel panel, in RenderState state )
		{
			var style = panel.ComputedStyle;
			if ( style == null ) return;
			if ( !panel.HasBackdropFilter ) return;

			var rect = panel.Box.Rect;
			var opacity = panel.Opacity * state.RenderOpacity;
			var size = (rect.Width + rect.Height) * 0.5f;
			var color = Color.White.WithAlpha( opacity );

			var isLayered = LayerStack?.Count > 0;

			Graphics.Attributes.SetCombo( "D_LAYERED", isLayered ? 1 : 0 );

			Graphics.Attributes.Set( "BoxPosition", panel.Box.Rect.Position );
			Graphics.Attributes.Set( "BoxSize", panel.Box.Rect.Size );
			SetBorderRadius( style, size );

			Graphics.Attributes.Set( "Brightness", style.BackdropFilterBrightness.Value.GetPixels( 1.0f ) );
			Graphics.Attributes.Set( "Contrast", style.BackdropFilterContrast.Value.GetPixels( 1.0f ) );
			Graphics.Attributes.Set( "Saturate", style.BackdropFilterSaturate.Value.GetPixels( 1.0f ) );
			Graphics.Attributes.Set( "Sepia", style.BackdropFilterSepia.Value.GetPixels( 1.0f ) );
			Graphics.Attributes.Set( "Invert", style.BackdropFilterInvert.Value.GetPixels( 1.0f ) );
			Graphics.Attributes.Set( "HueRotate", style.BackdropFilterHueRotate.Value.GetPixels( 1.0f ) );
			Graphics.Attributes.Set( "BlurScale", style.BackdropFilterBlur.Value.GetPixels( 1.0f ) );

			//
			// Update the frame buffer. Possible optimizations:
			//
			// * Copy only the rect that we need
			// * Only update once per common parent (more difficult - layered children wouldn't grab each other)
			// * Maybe only update to blur the game background or when explicitly forced?
			//
			// This all said I didn't see any performance issues calling this on EVERY panel with a background
			// 
			CopyFrameBufferCached( rect );

			Graphics.Attributes.SetComboEnum( "D_BLENDMODE", OverrideBlendMode );
			Graphics.DrawQuad( rect, Material.UI.BackdropFilter, color );
		}
	}
}
