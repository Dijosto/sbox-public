using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sandbox.Twitch
{
	internal partial class TwitchAPI
	{
		internal class CreateClipRequest
		{
			[JsonPropertyName( "broadcaster_id" )]
			public string BroadcasterId { get; set; }

			[JsonPropertyName( "has_delay" )]
			public bool HasDelay { get; set; }
		}

		internal class CreateClipResponse
		{
			[JsonPropertyName( "edit_url" )]
			public string EditUrl { get; set; }

			[JsonPropertyName( "id" )]
			public string Id { get; set; }
		}

		public class CreateClipsResponse
		{
			[JsonPropertyName( "data" )]
			public CreateClipResponse[] Clips { get; set; }

			public CreateClipResponse FirstOrDefault() => Clips != null && Clips.Length > 0 ? Clips.FirstOrDefault() : null;
		}

		public async Task<CreateClipResponse> CreateClip( string userId, bool hasDelay )
		{
			var response = await Post( $"/clips?broadcaster_id={userId}", JsonSerializer.Serialize( new CreateClipRequest
			{
				BroadcasterId = userId,
				HasDelay = hasDelay,
			} ) );

			var json = await response.Content.ReadAsStringAsync();

			var clips = JsonSerializer.Deserialize<CreateClipsResponse>( json );

			return clips.FirstOrDefault();
		}
	}
}
