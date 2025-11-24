using NativeEngine;
using Sandbox;

namespace Editor;

/// <summary>
/// Provides information about currently resident textures on the GPU
/// </summary>
public struct TextureResidencyInfo
{
	public enum TextureDimension
	{
		_1D,
		_2D,
		_2DArray,
		_3D,
		Cube,
		CubeArray,
		Buffer
	}

	public struct Desc
	{
		public int Width;
		public int Height;
		public int Depth;
		public long MemorySize;
	}

	public string Name;
	public TextureDimension Dimension;
	public ImageFormat Format;
	public Desc Loaded;
	public Desc Disk;

	static TextureResidencyInfo FromNative( string name, ITexture texture )
	{
		var loadedDesc = g_pRenderDevice.GetTextureDesc( texture );
		var diskDesc = g_pRenderDevice.GetOnDiskTextureDesc( texture );

		var loadedMemorySize = loadedDesc.ArrayCount * ImageLoader.GetMemRequired( loadedDesc.m_nWidth, loadedDesc.m_nHeight, loadedDesc.Depth, loadedDesc.m_nNumMipLevels, loadedDesc.m_nImageFormat );
		var diskMemorySize = diskDesc.ArrayCount * ImageLoader.GetMemRequired( diskDesc.m_nWidth, diskDesc.m_nHeight, diskDesc.Depth, diskDesc.m_nNumMipLevels, diskDesc.m_nImageFormat );

		var flags = loadedDesc.m_nFlags;
		var dimension = (flags & RuntimeTextureSpecificationFlags.TSPEC_CUBE_TEXTURE) != 0
		? (flags & RuntimeTextureSpecificationFlags.TSPEC_TEXTURE_ARRAY) != 0 ? TextureDimension.CubeArray : TextureDimension.Cube
		: (flags & RuntimeTextureSpecificationFlags.TSPEC_VOLUME_TEXTURE) != 0 ? TextureDimension._3D
		: (flags & RuntimeTextureSpecificationFlags.TSPEC_TEXTURE_ARRAY) != 0 ? TextureDimension._2DArray
		: TextureDimension._2D;

		return new()
		{
			Name = name,
			Format = loadedDesc.m_nImageFormat,
			Dimension = dimension,
			Loaded =
			{
				Width = loadedDesc.m_nWidth,
				Height = loadedDesc.m_nHeight,
				Depth = loadedDesc.m_nDepth,
				MemorySize = loadedMemorySize
			},
			Disk =
			{
				Width = diskDesc.m_nWidth,
				Height = diskDesc.m_nHeight,
				Depth = diskDesc.m_nDepth,
				MemorySize = diskMemorySize
			},
		};
	}

	/// <summary>
	/// Get info about all resident textures
	/// </summary>
	public static IEnumerable<TextureResidencyInfo> GetAll()
	{
		var ret = new List<TextureResidencyInfo>();

		var names = CUtlVectorString.Create( 8, 8 );
		var list = CUtlVectorTexture.Create( 8, 8 );
		g_pRenderDevice.GetTextureResidencyInfo( list, names );

		var count = list.Count();

		for ( int i = 0; i < count; i++ )
		{
			var texture = list.Element( i );
			var name = names.Element( i );

			ret.Add( FromNative( name, texture ) );

			// We just want to observe...
			texture.DestroyStrongHandle();
		}

		list.DeleteThis();
		names.DeleteThis();

		return ret;
	}
}
