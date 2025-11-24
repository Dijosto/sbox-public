using Sandbox.Engine;

namespace Sandbox.UI;

internal partial class PanelRenderer
{
	/// <summary>
	/// Software scissor, panels outside of this should not be rendered
	/// </summary>
	internal Rect Scissor;

	/// <summary>
	/// Scissor passed to gpu shader to be transformed
	/// </summary>
	internal GPUScissor ScissorGPU;
	internal struct GPUScissor
	{
		public Rect Rect;
		public Vector4 CornerRadius;
		public Matrix Matrix;
	}

	internal class ClipScope : IDisposable
	{
		Rect Previous;
		GPUScissor PreviousGPU;

		public ClipScope( Rect scissorRect, Vector4 cornerRadius, Matrix globalMatrix )
		{
			var renderer = GlobalContext.Current.UISystem.Renderer;

			Previous = renderer.Scissor;
			PreviousGPU = renderer.ScissorGPU;

			renderer.ScissorGPU.Rect = new Rect()
			{
				Left = Math.Max( scissorRect.Left, PreviousGPU.Rect.Left ),
				Top = Math.Max( scissorRect.Top, PreviousGPU.Rect.Top ),
				Right = Math.Min( scissorRect.Right, PreviousGPU.Rect.Right ),
				Bottom = Math.Min( scissorRect.Bottom, PreviousGPU.Rect.Bottom ),
			};

			renderer.ScissorGPU.CornerRadius = cornerRadius;
			renderer.ScissorGPU.Matrix = globalMatrix;

			SetScissorAttributes( renderer.ScissorGPU );

			var tl = globalMatrix.Transform( scissorRect.TopLeft );
			var tr = globalMatrix.Transform( scissorRect.TopRight );
			var bl = globalMatrix.Transform( scissorRect.BottomLeft );
			var br = globalMatrix.Transform( scissorRect.BottomRight );

			var min = Vector2.Min( Vector2.Min( tl, tr ), Vector2.Min( bl, br ) );
			var max = Vector2.Max( Vector2.Max( tl, tr ), Vector2.Max( bl, br ) );

			scissorRect = new Rect( min, max - min );

			renderer.Scissor = new Rect()
			{
				Left = Math.Max( scissorRect.Left, Previous.Left ),
				Top = Math.Max( scissorRect.Top, Previous.Top ),
				Right = Math.Min( scissorRect.Right, Previous.Right ),
				Bottom = Math.Min( scissorRect.Bottom, Previous.Bottom ),
			};

		}

		public void Dispose()
		{
			var renderer = GlobalContext.Current.UISystem.Renderer;
			renderer.Scissor = Previous;
			renderer.ScissorGPU = PreviousGPU;

			SetScissorAttributes( renderer.ScissorGPU );
		}

		void SetScissorAttributes( GPUScissor scissor )
		{
			if ( scissor.Rect.Width == 0 && scissor.Rect.Height == 0 )
			{
				Graphics.Attributes.Set( "HasScissor", 0 );
				return;
			}

			Graphics.Attributes.Set( "ScissorRect", scissor.Rect.ToVector4() );
			Graphics.Attributes.Set( "ScissorCornerRadius", scissor.CornerRadius );
			Graphics.Attributes.Set( "ScissorTransformMat", scissor.Matrix );
			Graphics.Attributes.Set( "HasScissor", 1 );
		}
	}

	public ClipScope Clip( Panel panel )
	{
		if ( (panel.ComputedStyle?.Overflow ?? OverflowMode.Visible) == OverflowMode.Visible )
			return null;

		var size = (panel.Box.Rect.Width + panel.Box.Rect.Height) * 0.5f;
		var borderRadius = new Vector4( panel.ComputedStyle.BorderTopLeftRadius?.GetPixels( size ) ?? 0, panel.ComputedStyle.BorderTopRightRadius?.GetPixels( size ) ?? 0, panel.ComputedStyle.BorderBottomLeftRadius?.GetPixels( size ) ?? 0, panel.ComputedStyle.BorderBottomRightRadius?.GetPixels( size ) ?? 0 );

		return new ClipScope( panel.Box.ClipRect, borderRadius, panel.GlobalMatrix ?? Matrix.Identity );
	}

	void InitScissor( Rect rect )
	{
		Scissor = rect;
		ScissorGPU = new() { Rect = rect };

		Graphics.Attributes.Set( "ScissorRect", rect.ToVector4() );
		Graphics.Attributes.Set( "ScissorCornerRadius", Vector4.Zero );
		Graphics.Attributes.Set( "ScissorTransformMat", Matrix.Identity );
		Graphics.Attributes.Set( "HasScissor", 1 );
	}

	[ConVar( ConVarFlags.Protected, Help = "Enable/disabling culling panel rendering based on overflow != visible. Turning this on or off should never affect visibility because the actual rendering should be culled using stencils. If it does, then the culling logic is wrong." )]
	public static bool ui_cull { get; set; } = true;

	/// <summary>
	/// Quick check to see if a panel should be culled based on the current scissor
	/// </summary>
	bool ShouldEarlyCull( Panel panel )
	{
		//
		// This shit should be fast, so don't do complicated shit here
		// Keep it simple AABB, doesn't matter if we miss some overflow because the shader will clear up anything else
		//

		if ( !ui_cull ) return false;

		//
		// Can't clip contents panels
		//
		if ( panel.ComputedStyle.Display == DisplayMode.Contents )
			return false;

		var rect = panel.Box.Rect;

		//
		// Grow our rect by any shadows we might have
		//
		if ( panel.ComputedStyle.BoxShadow is ShadowList shadows && shadows.Count > 0 )
		{
			for ( int i = 0; i < shadows.Count; i++ )
			{
				var shadow = shadows[i];
				if ( shadow.Inset ) continue;

				var shadowRect = panel.Box.Rect + new Vector2( shadow.OffsetX, shadow.OffsetY );
				rect.Add( shadowRect.Grow( shadow.Spread ) );
			}
		}

		//
		// AABB transform
		//
		if ( panel.GlobalMatrix.HasValue )
		{
			var mat = panel.GlobalMatrix.Value;
			var tl = mat.Transform( rect.TopLeft );
			var tr = mat.Transform( rect.TopRight );
			var bl = mat.Transform( rect.BottomLeft );
			var br = mat.Transform( rect.BottomRight );

			var min = Vector2.Min( Vector2.Min( tl, tr ), Vector2.Min( bl, br ) );
			var max = Vector2.Max( Vector2.Max( tl, tr ), Vector2.Max( bl, br ) );

			rect = new Rect( min, max - min );
		}

		return !Scissor.IsInside( rect );
	}

}
