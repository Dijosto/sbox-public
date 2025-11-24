using Sandbox.Services;
using Sandbox.Twitch;

namespace Sandbox.Engine
{
	internal static partial class Streamer
	{
		internal static IStreamService CurrentService;
		internal static StreamBroadcast CurrentBroadcast;

		internal static ServiceToken _serviceToken;
		internal static ServiceToken ServiceToken
		{
			get
			{
				if ( _serviceToken.Token is null )
					throw new System.Exception( "No service token" );

				return _serviceToken;
			}

			private set => _serviceToken = value;
		}

		/// <summary>
		/// Your own username
		/// </summary>
		public static string Username => ServiceToken.Name;

		/// <summary>
		/// Your own user id
		/// </summary>
		public static string UserId => ServiceToken.Id;

		internal static string Token => ServiceToken.Token;

		/// <summary>
		/// The service type (ie "Twitch")
		/// </summary>
		public static StreamService ServiceType { get; private set; } = StreamService.None;

		/// <summary>
		/// Are we connected to a service
		/// </summary>
		public static bool IsActive => CurrentService != null;

		internal static void RunEvent( string name )
		{
			IMenuDll.Current?.RunEvent( name );
		}

		internal static void RunEvent( string name, object argument )
		{
			IMenuDll.Current?.RunEvent( name, argument );
		}

		static async Task<bool> Init( IStreamService service, ServiceToken token )
		{
			_serviceToken = token;
			CurrentService = service;

			ServiceType = CurrentService != null ? CurrentService.ServiceType : StreamService.None;

			var success = await CurrentService.Connect();
			if ( !success )
			{
				ServiceType = StreamService.None;
				CurrentService = null;
				Log.Info( "Connection failed." );
				return false;
			}

			Log.Info( "Connected" );
			return true;
		}

		internal static async Task<bool> Init( StreamService serviceType )
		{
			if ( CurrentService != null )
			{
				Log.Warning( "Tried to start stream but already connected" );
				return false;
			}

			Log.Info( "Getting Service Token.." );
			IStreamService service = null;
			var token = await GetLinkedService( serviceType );
			if ( token.Token is null )
			{
				Log.Warning( $"Couldn't retrieve token for {serviceType} (open https://sbox.facepunch.com/link)" );
				return false;
			}

			Log.Info( "Creating Service.." );

			switch ( serviceType )
			{
				case StreamService.Twitch:
					service = new TwitchService();
					break;
			}

			return await Init( service, token );
		}

		private static async Task<ServiceToken> GetLinkedService( StreamService serviceType )
		{
			var serviceName = $"{serviceType}";
			serviceName = serviceName.ToLower();

			try
			{
				return await Sandbox.Backend.Account.GetService( serviceName );
			}
			catch ( System.Exception )
			{
				return default;
			}
		}

		internal static void Shutdown()
		{
			CurrentService?.Disconnect();
			CurrentService = null;
		}
	}
}
