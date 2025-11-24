using System;
using System.Collections.Generic;

namespace Sandbox.UI
{
	public partial class Styles
	{
		public struct GradientColorOffset
		{
			public Color color;
			public float? offset;

			public override int GetHashCode()
			{
				return HashCode.Combine( color, offset );
			}
		}
		public struct GradientGenerator
		{
			public GradientColorOffset from;
			public GradientColorOffset to;
		}

		private List<GradientGenerator> ParseGradient( string token )
		{
			var gradientGenerators = new List<GradientGenerator>();

			Color? lastColor = null;
			float? lastOffset = null;

			var p = new Parse( token );

			// Parse the color values
			while ( !p.IsEnd )
			{
				p = p.SkipWhitespaceAndNewlines();

				// Read up to a comma or end of the text within the brackets
				var w = p.ReadSentence();
				var wp = new Parse( w );

				// First parse the color
				var c = Color.Parse( ref wp );
				if ( !c.HasValue )
				{
					Log.Error( $"Cannot read a color from '{w}'" );
					break;
				}

				wp = wp.SkipWhitespaceAndNewlines();

				// Then optionally parse the stop position
				float? offset = null;
				if ( wp.IsDigit && wp.TryReadFloat( out var stop ) )
				{
					if ( !wp.Is( '%' ) )
					{
						Log.Error( $"Only percent stop values are supported: '{w}'" );
						break;
					}

					wp.Pointer++;
					offset = stop / 100;
				}

				wp = wp.SkipWhitespaceAndNewlines();

				if ( !wp.IsEnd )
				{
					Log.Error( $"Extra text found after color stop: '{w}'" );
					break;
				}

				if ( c.HasValue )
				{
					if ( lastColor.HasValue )
					{
						var gradient = new GradientGenerator();
						gradient.from.color = lastColor.Value;
						gradient.from.offset = lastOffset;
						gradient.to.color = c.Value;
						gradient.to.offset = offset;

						gradientGenerators.Add( gradient );
					}

					lastColor = c;
					lastOffset = offset;
				}

				if ( p.Is( ',' ) )
				{
					p.Pointer++;
				}
				else
				{
					break;
				}
			}

			if ( gradientGenerators.Count == 0 )
			{
				var solidColor = lastColor ?? Color.Black;
				var item = new GradientGenerator();
				item.from = new GradientColorOffset()
				{
					color = solidColor,
					offset = 0
				};
				item.to = new GradientColorOffset()
				{
					color = solidColor,
					offset = 1
				};

				gradientGenerators.Add( item );

				return gradientGenerators;
			}

			// Set the distance properties that were not initialized
			float perSliceDistance = 1.0f / (float)gradientGenerators.Count;

			for ( int i = 0; i < gradientGenerators.Count; i++ )
			{
				var gradientGenerator = gradientGenerators[i];

				if ( !gradientGenerator.from.offset.HasValue )
					gradientGenerator.from.offset = (float)i * perSliceDistance;
				if ( !gradientGenerator.to.offset.HasValue )
					gradientGenerator.to.offset = (float)(i + 1) * perSliceDistance;

				gradientGenerators[i] = gradientGenerator;
			}

			// fill in the gap if we weren't given a final stop point
			var lastGenerator = gradientGenerators[^1];
			if ( lastGenerator.to.offset.Value < 1 )
			{
				gradientGenerators.Add( new GradientGenerator
				{
					from = lastGenerator.to,
					to = new GradientColorOffset
					{
						color = lastGenerator.to.color,
						offset = 1,
					},
				} );
			}

			return gradientGenerators;
		}

		private int CalcOptimalGradientWidth()
		{
			var width = BackgroundSizeX?.GetPixels( 1f ) ?? Width?.GetPixels( 1f ) ?? 0f;
			var height = BackgroundSizeY?.GetPixels( 1f ) ?? Height?.GetPixels( 1f ) ?? 0f;

			var calcWidth = MathF.Max( width, height );
			var gradientWidth = Math.Clamp( (int)calcWidth, 256, 2048 );

			return gradientWidth;
		}

		private Color32 LerpPremultiplied( Color32 from, Color32 to, float t )
		{
			// Convert colors
			var colA = from.ToColor( true );
			var colB = to.ToColor( true );

			// Premultiply RGB by alpha
			float r1 = colA.r * colA.a;
			float g1 = colA.g * colA.a;
			float b1 = colA.b * colA.a;

			float r2 = colB.r * colB.a;
			float g2 = colB.g * colB.a;
			float b2 = colB.b * colB.a;

			// Lerp premultiplied values
			float r = r1 + t * (r2 - r1);
			float g = g1 + t * (g2 - g1);
			float b = b1 + t * (b2 - b1);

			// Interpolate alpha using quadratic easing
			float a = colA.a + (colB.a - colA.a) * (t * t);

			if ( a > 0.0001f ) // avoid division by zero
			{
				r /= a;
				g /= a;
				b /= a;
			}

			var col = new Color( r, g, b, a );

			return col.ToColor32( true );
		}

		private byte[] GenerateGradient( string token, int gradientWidth )
		{
			var gradientGenerators = ParseGradient( token );

			byte[] gradientData = new byte[gradientWidth * 4];

			// Actually generate gradient data now
			foreach ( var gradient in gradientGenerators )
			{
				var fromColor = gradient.from.color.ToColor32();
				var toColor = gradient.to.color.ToColor32();

				int fromPixel = (int)((gradient.from.offset.Value) * gradientWidth);
				int toPixel = (int)((gradient.to.offset.Value) * gradientWidth);

				for ( int i = fromPixel; i < toPixel; i++ )
				{
					float j = (float)(i - fromPixel) / (float)(toPixel - fromPixel);

					var color = LerpPremultiplied( fromColor, toColor, j ).ToColor();

					gradientData[(i * 4) + 0] = (byte)(color.r * 255);
					gradientData[(i * 4) + 1] = (byte)(color.g * 255);
					gradientData[(i * 4) + 2] = (byte)(color.b * 255);
					gradientData[(i * 4) + 3] = (byte)(color.a * 255);
				}
			}

			return gradientData;
		}

		Texture GenerateConicGradientTexture( string token )
		{
			var p = new Parse( token );
			Vector2 centerOffset;

			// Temporary, this can be changed by client too
			centerOffset = new Vector2( 0.5f, 0.5f );

			var gradientWidth = CalcOptimalGradientWidth();
			byte[] gradientLUT = GenerateGradient( p.ReadRemaining(), gradientWidth );

			gradientWidth = gradientLUT.Length / 4;

			byte[] gradientData = new byte[gradientWidth * gradientWidth * 4];

			// Wrap the 1D linear gradient we have calculated into a cone
			for ( int x = 0; x < gradientWidth; x++ )
			{
				for ( int y = 0; y < gradientWidth; y++ )
				{
					Vector2 pos = new Vector2( (float)x / gradientWidth, (float)y / gradientWidth );
					var distance = (Math.Atan2( pos.y - centerOffset.y, pos.x - centerOffset.y ) + Math.PI) / (Math.PI * 2.0f);

					int s = Math.Clamp( gradientWidth - (int)(distance * gradientWidth), 0, gradientWidth - 1 );
					int outS = ((x * gradientWidth) + y) * 4;

					gradientData[outS + 0] = gradientLUT[(s * 4) + 0];
					gradientData[outS + 1] = gradientLUT[(s * 4) + 1];
					gradientData[outS + 2] = gradientLUT[(s * 4) + 2];
					gradientData[outS + 3] = gradientLUT[(s * 4) + 3];
				}
			}

			var gradientTexture = Texture.Create( gradientWidth, gradientWidth )
			.WithName( "conic-gradient" )
			.WithData( gradientData )
			.Finish();

			return gradientTexture;
		}

		Texture GenerateRadialGradientTexture( string token )
		{
			var p = new Parse( token );
			Vector2 centerOffset;

			// https://developer.mozilla.org/en-US/docs/Web/CSS/radial-gradient()
			//
			p.SkipWhitespaceAndNewlines();
			if ( p.Is( "closest-side", 0, true ) )
			{

			}
			else if ( p.Is( "closest-corner", 0, true ) )
			{

			}
			else if ( p.Is( "farthest-side", 0, true ) )
			{

			}
			else if ( p.Is( "farthest-corner", 0, true ) )
			{

			}

			// Temporary, this can be changed by client too
			centerOffset = new Vector2( 0.5f, 0.5f );

			var gradientWidth = CalcOptimalGradientWidth();
			byte[] gradientLUT = GenerateGradient( p.ReadRemaining(), gradientWidth );

			gradientWidth = gradientLUT.Length / 4;

			byte[] gradientData = new byte[gradientWidth * gradientWidth * 4];

			// Wrap the 1D linear gradient we have calculated into a radial
			for ( int x = 0; x < gradientWidth; x++ )
			{
				for ( int y = 0; y < gradientWidth; y++ )
				{
					Vector2 pos = new Vector2( (float)x / gradientWidth, (float)y / gradientWidth );
					var distance = Vector2.Distance( pos, centerOffset );

					int s = Math.Clamp( (int)(distance * gradientWidth), 0, gradientWidth - 1 );
					int outS = ((x * gradientWidth) + y) * 4;

					gradientData[outS + 0] = gradientLUT[(s * 4) + 0];
					gradientData[outS + 1] = gradientLUT[(s * 4) + 1];
					gradientData[outS + 2] = gradientLUT[(s * 4) + 2];
					gradientData[outS + 3] = gradientLUT[(s * 4) + 3];
				}
			}

			var gradientTexture = Texture.Create( gradientWidth, gradientWidth )
			.WithName( "radial-gradient" )
			.WithData( gradientData )
			.Finish();

			return gradientTexture;
		}

		Texture GenerateLinearGradientTexture( string token, out float angle )
		{
			angle = -1;

			var p = new Parse( token );

			var restoreP = p;
			var angleStr = p.ReadSentence();
			if ( TryParseAngle( angleStr, out angle ) )
			{
				p.Pointer++; // comma
			}
			else
			{
				p = restoreP;
			}

			var gradientWidth = CalcOptimalGradientWidth();
			byte[] gradientData = GenerateGradient( p.ReadRemaining(), gradientWidth );

			var gradientTexture = Texture.Create( 1, gradientData.Length / 4 )
			.WithName( "linear-gradient" )
			.WithData( gradientData )
			.Finish();

			return gradientTexture;
		}
	}
}
