using System;

namespace TestTexture;

[TestClass]
public class TextureTest
{
	[TestMethod]
	public void Copy()
	{
		var src = Texture.Create( 1, 1 ).Finish();
		var dst = Texture.Create( 1, 1 ).Finish();

		try
		{
			Graphics.CopyTexture( src, dst );
		}
		catch ( Exception ex )
		{
			Assert.Fail( $"Valid CopyTexture call threw an exception: {ex}" );
		}

		try
		{
			Graphics.CopyTexture( src, dst, srcMipSlice: 0, srcArraySlice: 0, dstMipSlice: 0, dstArraySlice: 0 );
		}
		catch ( Exception ex )
		{
			Assert.Fail( $"Valid CopyTexture call threw an exception: {ex}" );
		}

		// Out-of-range mip on src
		Assert.ThrowsException<ArgumentException>( () =>
		{
			Graphics.CopyTexture( src, dst, srcMipSlice: 1, srcArraySlice: 0, dstMipSlice: 0, dstArraySlice: 0 );
		} );

		// Out-of-range array slice on src
		Assert.ThrowsException<ArgumentException>( () =>
		{
			Graphics.CopyTexture( src, dst, srcMipSlice: 0, srcArraySlice: 1, dstMipSlice: 0, dstArraySlice: 0 );
		} );

		// Out-of-range mip on dst
		Assert.ThrowsException<ArgumentException>( () =>
		{
			Graphics.CopyTexture( src, dst, srcMipSlice: 0, srcArraySlice: 0, dstMipSlice: 1, dstArraySlice: 0 );
		} );

		// Out-of-range array slice on dst
		Assert.ThrowsException<ArgumentException>( () =>
		{
			Graphics.CopyTexture( src, dst, srcMipSlice: 0, srcArraySlice: 0, dstMipSlice: 0, dstArraySlice: 1 );
		} );
	}
}
