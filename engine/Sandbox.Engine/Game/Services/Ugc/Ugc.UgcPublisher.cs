namespace Sandbox.Services;

internal static partial class Ugc
{
	/// <summary>
	/// A class that can be used to update a workshop file.
	/// This is inaccessible in game. Instead, use the publisher modal.
	/// </summary>
	public class UgcPublisher
	{
		NativeEngine.CUgcUpdate native;

		internal UgcPublisher( NativeEngine.CUgcUpdate native )
		{
			this.native = native;
		}

		~UgcPublisher()
		{
			MainThread.QueueDispose( native );
		}

		public ulong ItemId => native.GetPublishedFileId();
		public bool NeedsLegalAgreement => native.m_bNeedsLegalAgreement;
		public bool IsPublishing => native.m_submitted;
		public bool IsFinished => native.m_complete;
		public float PercentComplete => native.GetProgressPercent();

		public void SetTitle( string value ) => native.SetTitle( value );
		public void SetDescription( string value ) => native.SetDescription( value );
		public void SetPreviewImage( string value ) => native.SetPreviewImage( value );
		public void SetKeyValue( string key, string value ) => native.AddKeyValueTag( key, value );
		public void SetTag( string tag ) => native.SetTag( tag );
		public void SetContentFile( string fullPath ) => native.SetContentFolder( fullPath );
		public void SetMetaData( string metadata ) => native.SetMetadata( metadata );
		public void SetVisibility( Storage.Visibility value ) => native.SetVisibility( (int)value );

		public async Task<bool> Submit( string changeNotes = "" )
		{
			if ( !native.Submit( changeNotes ) )
				return false;

			while ( !native.m_complete )
			{
				await Task.Delay( 10 );
			}

			return true;
		}
	}

}
