using System;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Twitch;

namespace Sandbox
{
	public struct StreamPoll
	{
		public struct Choice
		{
			public string Id { get; internal set; }
			public string Title { get; internal set; }
			public int Votes { get; internal set; }
			public int ChannelPointsVotes { get; internal set; }
			public int BitsVotes { get; internal set; }
		}

		internal StreamPoll( TwitchAPI.PollResponse poll )
		{
			Id = poll.BroadcasterId;
			BroadcasterId = poll.BroadcasterLogin;
			BroadcasterName = poll.BroadcasterName;
			BroadcasterLogin = poll.BroadcasterLogin;
			Title = poll.Title;
			BitsVotingEnabled = poll.BitsVotingEnabled;
			BitsPerVote = poll.BitsPerVote;
			ChannelPointsVotingEnabled = poll.ChannelPointsVotingEnabled;
			ChannelPointsPerVote = poll.ChannelPointsPerVote;
			Status = poll.Status;
			Duration = poll.Duration;
			StartedAt = DateTimeOffset.Parse( poll.StartedAt );
			EndedAt = DateTimeOffset.Parse( poll.EndedAt );
			Choices = poll.Choices.Select( choice => new Choice
			{
				Id = choice.Id,
				Title = choice.Title,
				Votes = choice.Votes,
				ChannelPointsVotes = choice.ChannelPointsVotes,
				BitsVotes = choice.BitsVotes,
			} )
			.ToArray();
		}

		/// <summary>
		/// End this poll, you can optionally archive the poll, otherwise just terminate it
		/// </summary>
		public Task<StreamPoll> End( bool archive = true )
		{
			return Engine.Streamer.CurrentService?.EndPoll( BroadcasterId, Id, archive );
		}

		public string Id { get; internal set; }
		public string BroadcasterId { get; internal set; }
		public string BroadcasterName { get; internal set; }
		public string BroadcasterLogin { get; internal set; }
		public string Title { get; internal set; }
		public StreamPoll.Choice[] Choices { get; internal set; }
		public bool BitsVotingEnabled { get; internal set; }
		public int BitsPerVote { get; internal set; }
		public bool ChannelPointsVotingEnabled { get; internal set; }
		public int ChannelPointsPerVote { get; internal set; }
		public string Status { get; internal set; }
		public int Duration { get; internal set; }
		public DateTimeOffset StartedAt { get; internal set; }
		public DateTimeOffset EndedAt { get; internal set; }
	}
}
