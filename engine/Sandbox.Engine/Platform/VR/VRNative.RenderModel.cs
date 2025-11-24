namespace Sandbox.VR;

/// <summary>
/// Native helpers for VR
/// </summary>
static partial class VRNative
{
	private struct RenderModelInfo
	{
		public List<Vertex> Vertices = new();
		public List<ushort> Indices = new();
		public int DiffuseTextureId = 0;

		public RenderModelInfo()
		{

		}
	}

	private static RenderModelInfo? GetRenderModelInfo( string renderModelName )
	{
		return null;
	}

	private static Texture GetRenderModelTexture( int textureId )
	{
		return null;
	}

	internal static Model GetRenderModel( string renderModelName )
	{
		return null;
	}
}
