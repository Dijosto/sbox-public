using Steamworks.Data;
using System;

namespace Sandbox;

/// <summary>
/// Utils for uploading assets to Steam. This is wholely for clothing right now.
/// </summary>
static class WorkshopUtils
{
	/// <summary>
	/// Is this allowed to be uploaded?
	/// </summary>
	/// <param name="asset"></param>
	/// <returns></returns>
	private static bool IsAllowed( Asset asset )
	{
		if ( asset.AssetType.FileExtension == "clothing" )
			return true;
		return false;
	}

	/// <summary>
	/// Creates some temporary files for the workshop addon so we can track them
	/// </summary>
	/// <param name="asset"></param>
	/// <param name="itemId"></param>
	private static void CreateTemporaryFilesInternal( Asset asset, PublishedFileId itemId )
	{
		Editor.FileSystem.Transient.CreateDirectory( $"{asset.Name}" );

		var addon = asset.Publishing.CreateTemporaryProject();

		Editor.FileSystem.Transient.WriteJson( $"{asset.Name}/item.json", new WorkshopItemMetaData()
		{
			PackageIdent = addon.Config.FullIdent,
			Title = addon.Config.Title,
			WorkshopId = itemId
		} );
	}

	/// <inheritdoc cref="CreateTemporaryFilesInternal(Asset, PublishedFileId)" />
	internal static IDisposable CreateTemporaryFiles( Asset asset, PublishedFileId itemId )
	{
		// Just in-case
		DestroyTemporaryFiles( asset );

		CreateTemporaryFilesInternal( asset, itemId );
		return new Sandbox.Utility.DisposeAction( () => DestroyTemporaryFiles( asset ) );
	}

	/// <summary>
	/// Destroys temporary files made for the workshop addon
	/// </summary>
	/// <param name="asset"></param>
	private static void DestroyTemporaryFiles( Asset asset )
	{
		if ( Editor.FileSystem.Transient.DirectoryExists( asset.Name ) )
		{
			Editor.FileSystem.Transient.DeleteDirectory( asset.Name, true );
		}
	}


	static bool g_bNeedsLegalAgreement = false;

	/// <summary>
	/// Do we need to fill out the legal agreement in Steam?
	/// </summary>
	/// <returns></returns>
	public static bool NeedsLegalAgreement()
	{
		return g_bNeedsLegalAgreement;
	}

	/// <summary>
	/// Uploads an asset to the steam workshop and embeds the published id in the asset's metadata
	/// </summary>
	/// <param name="asset"></param>
	/// <returns></returns>
	public static async Task<bool> UploadAsset( Asset asset )
	{
		if ( !IsAllowed( asset ) )
			return false;

		// TODO: this should be generic to grab the name instead of getting a clothing resource.
		if ( !asset.TryLoadResource<Clothing>( out var clothing ) )
			return false;

		var itemId = asset.MetaData.Get( "WorkshopId", 0UL );

		var item = itemId > 0 ? Services.Ugc.OpenItem( itemId ) : await Services.Ugc.CreateMtxItem();

		item.SetTitle( clothing.Title );

		if ( !string.IsNullOrEmpty( clothing.Icon.Path ) )
		{
			var ico = FileSystem.Mounted.GetFullPath( clothing.Icon.Path );
			item.SetPreviewImage( ico );
		}

		itemId = item.ItemId;

		using ( CreateTemporaryFiles( asset, itemId ) )
		{
			// The content is just a json file with data in it
			var fullPath = Editor.FileSystem.Transient.GetFullPath( $"{asset.Name}" );
			//.Replace( "\\", "/" );

			item.SetContentFile( fullPath );

			var result = await item.Submit();

			g_bNeedsLegalAgreement = item.NeedsLegalAgreement;

			if ( result )
			{
				// Save workshop id if we successfully uploaded to the workshop
				asset.MetaData.Set( "WorkshopId", itemId );
			}

			return result;
		}
	}
}
