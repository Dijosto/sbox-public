using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sandbox.Twitch
{
	internal partial class TwitchAPI
	{
		public class ChannelResponse
		{
			[JsonPropertyName( "broadcaster_id" )]
			public string BroadcasterId { get; set; }

			[JsonPropertyName( "broadcaster_login" )]
			public string BroadcasterLogin { get; set; }

			[JsonPropertyName( "broadcaster_name" )]
			public string BroadcasterName { get; set; }

			[JsonPropertyName( "broadcaster_language" )]
			public string BroadcasterLanguage { get; set; }

			[JsonPropertyName( "game_id" )]
			public string GameId { get; set; }

			[JsonPropertyName( "game_name" )]
			public string GameName { get; set; }

			[JsonPropertyName( "title" )]
			public string Title { get; set; }

			[JsonPropertyName( "delay" )]
			public int Delay { get; set; }
		}

		public class ChannelsResponse
		{
			[JsonPropertyName( "data" )]
			public ChannelResponse[] Channels { get; set; }

			public ChannelResponse FirstOrDefault() => Channels != null && Channels.Length > 0 ? Channels.FirstOrDefault() : null;
		}

		public async Task<ChannelResponse> GetChannel( string userId )
		{
			var response = await Get<ChannelsResponse>( $"/channels?broadcaster_id={userId}" );
			return response?.FirstOrDefault();
		}

		public async void SetChannelGame( string userId, string gameId )
		{
			await Patch( $"/channels?broadcaster_id={userId}", $"{{\"game_id\":\"{gameId}\"}}" );
		}

		public async void SetChannelLanguage( string userId, string language )
		{
			await Patch( $"/channels?broadcaster_id={userId}", $"{{\"broadcaster_language\":\"{language}\"}}" );
		}

		public async void SetChannelTitle( string userId, string title )
		{
			await Patch( $"/channels?broadcaster_id={userId}", $"{{\"title\":\"{title}\"}}" );
		}

		public async void SetChannelDelay( string userId, int delay )
		{
			await Patch( $"/channels?broadcaster_id={userId}", $"{{\"delay\":{delay}}}" );
		}
	}
}
