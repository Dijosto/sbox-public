using System;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Twitch;

namespace Sandbox
{
	public struct StreamPrediction
	{
		public struct Outcome
		{
			public string Id { get; internal set; }
			public string Title { get; internal set; }
			public int Users { get; internal set; }
			public int ChannelPoints { get; internal set; }
			public string Color { get; internal set; }
		}

		internal StreamPrediction( TwitchAPI.PredictionResponse prediction )
		{
			Id = prediction.Id;
			BroadcasterId = prediction.BroadcasterId;
			BroadcasterName = prediction.BroadcasterName;
			BroadcasterLogin = prediction.BroadcasterLogin;
			Title = prediction.Title;
			WinningOutcomeId = prediction.WinningOutcomeId;
			PredictionWindow = prediction.PredictionWindow;
			Status = prediction.Status;
			CreatedAt = DateTimeOffset.Parse( prediction.CreatedAt );
			EndedAt = DateTimeOffset.Parse( prediction.EndedAt );
			LockedAt = DateTimeOffset.Parse( prediction.LockedAt );
			Outcomes = prediction.Outcomes.Select( choice => new Outcome
			{
				Id = choice.Id,
				Title = choice.Title,
				Users = choice.Users,
				ChannelPoints = choice.ChannelPoints,
				Color = choice.Color,
			} )
			.ToArray();
		}

		/// <summary>
		/// Lock this prediction
		/// </summary>
		public Task<StreamPrediction> Lock()
		{
			return Engine.Streamer.CurrentService?.LockPrediction( BroadcasterId, Id );
		}

		/// <summary>
		/// Cancel this prediction
		/// </summary>
		public Task<StreamPrediction> Cancel()
		{
			return Engine.Streamer.CurrentService?.CancelPrediction( BroadcasterId, Id );
		}

		/// <summary>
		/// Resolve this prediction and choose winning outcome to pay out channel points
		/// </summary>
		public Task<StreamPrediction> Resolve()
		{
			return Engine.Streamer.CurrentService?.ResolvePrediction( BroadcasterId, Id, WinningOutcomeId );
		}

		public string Id { get; internal set; }
		public string BroadcasterId { get; internal set; }
		public string BroadcasterLogin { get; internal set; }
		public string BroadcasterName { get; internal set; }
		public string Title { get; internal set; }
		public string WinningOutcomeId { get; internal set; }
		public int PredictionWindow { get; internal set; }
		public string Status { get; internal set; }
		public DateTimeOffset CreatedAt { get; internal set; }
		public DateTimeOffset EndedAt { get; internal set; }
		public DateTimeOffset LockedAt { get; internal set; }
		public Outcome[] Outcomes { get; internal set; }
	}
}
