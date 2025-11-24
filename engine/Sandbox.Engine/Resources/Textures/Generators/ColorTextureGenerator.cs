using System.Threading;

namespace Sandbox.Resources;


/// <summary>
/// Generate a texture which is just a single color
/// </summary>
[Title( "Color" )]
[Icon( "palette" )]
[ClassName( "color" )]
[Expose]
public class ColorTextureGenerator : TextureGenerator
{
	[KeyProperty]
	public Color Color { get; set; } = Color.Magenta;

	protected override ValueTask<Texture> CreateTexture( Options options, CancellationToken ct )
	{
		var bitmap = new Bitmap( 1, 1, Color.IsHdr );
		bitmap.Clear( Color );
		return ValueTask.FromResult( bitmap.ToTexture() );
	}
}
