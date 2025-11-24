using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sandbox.Twitch
{
	internal partial class TwitchAPI
	{
		internal class CreatePredictionRequest
		{
			internal class Outcome
			{
				[JsonPropertyName( "title" )]
				public string Title { get; set; }
			}

			[JsonPropertyName( "broadcaster_id" )]
			public string BroadcasterId { get; set; }

			[JsonPropertyName( "title" )]
			public string Title { get; set; }

			[JsonPropertyName( "outcomes" )]
			public Outcome[] Outcomes { get; set; }

			[JsonPropertyName( "prediction_window" )]
			public int Duration { get; set; }
		}

		internal class EndPredictionRequest
		{
			[JsonPropertyName( "broadcaster_id" )]
			public string BroadcasterId { get; set; }

			[JsonPropertyName( "id" )]
			public string Id { get; set; }

			[JsonPropertyName( "status" )]
			public string Status { get; set; }

			[JsonPropertyName( "winning_outcome_id" )]
			public string WinningOutcomeId { get; set; }
		}

		internal class PredictionResponse
		{
			internal class Outcome
			{
				[JsonPropertyName( "id" )]
				public string Id { get; set; }

				[JsonPropertyName( "title" )]
				public string Title { get; set; }

				[JsonPropertyName( "users" )]
				public int Users { get; set; }

				[JsonPropertyName( "channel_points" )]
				public int ChannelPoints { get; set; }

				[JsonPropertyName( "color" )]
				public string Color { get; set; }
			}

			[JsonPropertyName( "id" )]
			public string Id { get; set; }

			[JsonPropertyName( "broadcaster_id" )]
			public string BroadcasterId { get; set; }

			[JsonPropertyName( "broadcaster_login" )]
			public string BroadcasterLogin { get; set; }

			[JsonPropertyName( "broadcaster_name" )]
			public string BroadcasterName { get; set; }

			[JsonPropertyName( "title" )]
			public string Title { get; set; }

			[JsonPropertyName( "winning_outcome_id" )]
			public string WinningOutcomeId { get; set; }

			[JsonPropertyName( "prediction_window" )]
			public int PredictionWindow { get; set; }

			[JsonPropertyName( "status" )]
			public string Status { get; set; }

			[JsonPropertyName( "created_at" )]
			public string CreatedAt { get; set; }

			[JsonPropertyName( "ended_at" )]
			public string EndedAt { get; set; }

			[JsonPropertyName( "locked_at" )]
			public string LockedAt { get; set; }

			[JsonPropertyName( "outcomes" )]
			public Outcome[] Outcomes { get; set; }
		}

		public async Task<PredictionResponse> CreatePrediction( string userId, string title, int duration, string firstOutcome, string secondOutcome )
		{
			var response = await Post( $"/predictions", JsonSerializer.Serialize( new CreatePredictionRequest
			{
				BroadcasterId = userId,
				Title = title,
				Duration = duration,
				Outcomes = new[]
				{
					new CreatePredictionRequest.Outcome { Title = firstOutcome },
					new CreatePredictionRequest.Outcome { Title = secondOutcome }
				},
			} ) );

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<PredictionResponse>( json );
		}

		public async Task<PredictionResponse> LockPrediction( string userId, string predictionId )
		{
			var response = await Post( $"/predictions", JsonSerializer.Serialize( new EndPredictionRequest
			{
				BroadcasterId = userId,
				Id = predictionId,
				Status = "LOCKED"
			} ) );

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<PredictionResponse>( json );
		}

		public async Task<PredictionResponse> CancelPrediction( string userId, string predictionId )
		{
			var response = await Post( $"/predictions", JsonSerializer.Serialize( new EndPredictionRequest
			{
				BroadcasterId = userId,
				Id = predictionId,
				Status = "CANCELED"
			} ) );

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<PredictionResponse>( json );
		}

		public async Task<PredictionResponse> ResolvePrediction( string userId, string predictionId, string winningOutcomeId )
		{
			var response = await Post( $"/predictions", JsonSerializer.Serialize( new EndPredictionRequest
			{
				BroadcasterId = userId,
				Id = predictionId,
				Status = "RESOLVED",
				WinningOutcomeId = winningOutcomeId,
			} ) );

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<PredictionResponse>( json );
		}
	}
}
