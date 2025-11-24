using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sandbox.Twitch
{
	internal partial class TwitchAPI
	{
		internal class CreatePollRequest
		{
			internal class Choice
			{
				[JsonPropertyName( "title" )]
				public string Title { get; set; }
			}

			[JsonPropertyName( "broadcaster_id" )]
			public string BroadcasterId { get; set; }

			[JsonPropertyName( "title" )]
			public string Title { get; set; }

			[JsonPropertyName( "choices" )]
			public Choice[] Choices { get; set; }

			[JsonPropertyName( "duration" )]
			public int Duration { get; set; }
		}

		internal class EndPollRequest
		{
			[JsonPropertyName( "broadcaster_id" )]
			public string BroadcasterId { get; set; }

			[JsonPropertyName( "id" )]
			public string Id { get; set; }

			[JsonPropertyName( "status" )]
			public string Status { get; set; }
		}

		internal class PollResponse
		{
			internal class Choice
			{
				[JsonPropertyName( "id" )]
				public string Id { get; set; }

				[JsonPropertyName( "title" )]
				public string Title { get; set; }

				[JsonPropertyName( "votes" )]
				public int Votes { get; set; }

				[JsonPropertyName( "channel_points_votes" )]
				public int ChannelPointsVotes { get; set; }

				[JsonPropertyName( "bits_votes" )]
				public int BitsVotes { get; set; }
			}

			[JsonPropertyName( "id" )]
			public string Id { get; set; }

			[JsonPropertyName( "broadcaster_id" )]
			public string BroadcasterId { get; set; }

			[JsonPropertyName( "broadcaster_name" )]
			public string BroadcasterName { get; set; }

			[JsonPropertyName( "broadcaster_login" )]
			public string BroadcasterLogin { get; set; }

			[JsonPropertyName( "title" )]
			public string Title { get; set; }

			[JsonPropertyName( "choices" )]
			public Choice[] Choices { get; set; }

			[JsonPropertyName( "bits_voting_enabled" )]
			public bool BitsVotingEnabled { get; set; }

			[JsonPropertyName( "bits_per_vote" )]
			public int BitsPerVote { get; set; }

			[JsonPropertyName( "channel_points_voting_enabled" )]
			public bool ChannelPointsVotingEnabled { get; set; }

			[JsonPropertyName( "channel_points_per_vote" )]
			public int ChannelPointsPerVote { get; set; }

			[JsonPropertyName( "status" )]
			public string Status { get; set; }

			[JsonPropertyName( "duration" )]
			public int Duration { get; set; }

			[JsonPropertyName( "started_at" )]
			public string StartedAt { get; set; }

			[JsonPropertyName( "ended_at" )]
			public string EndedAt { get; set; }
		}

		public async Task<PollResponse> CreatePoll( string userId, string title, int duration, string[] choices )
		{
			var response = await Post( $"/polls", JsonSerializer.Serialize( new CreatePollRequest
			{
				BroadcasterId = userId,
				Title = title,
				Duration = duration,
				Choices = choices.Select( x => new CreatePollRequest.Choice { Title = x } ).ToArray()
			} ) );

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<PollResponse>( json );
		}

		public async Task<PollResponse> EndPoll( string userId, string pollId, bool archive = false )
		{
			var response = await Post( $"/polls", JsonSerializer.Serialize( new EndPollRequest
			{
				BroadcasterId = userId,
				Id = pollId,
				Status = archive ? "ARCHIVED" : "TERMINATED",
			} ) );

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<PollResponse>( json );
		}
	}
}
