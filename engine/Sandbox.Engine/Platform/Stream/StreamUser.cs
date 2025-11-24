using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sandbox
{
	public struct StreamUser
	{
		public string Id { get; internal set; }
		public string Login { get; internal set; }
		public string DisplayName { get; internal set; }
		public string UserType { get; internal set; }
		public string BroadcasterType { get; internal set; }
		public string Description { get; internal set; }
		public string ProfileImageUrl { get; internal set; }
		public string OfflineImageUrl { get; internal set; }
		public int ViewCount { get; internal set; }
		public string Email { get; internal set; }
		public DateTimeOffset CreatedAt { get; internal set; }


		/// <summary>
		/// Get following "Who is following us"
		/// </summary>
		public Task<List<StreamUserFollow>> Following
		{
			get => Engine.Streamer.CurrentService?.GetUserFollowing( Id );
		}

		/// <summary>
		/// Get followers "Who are we following"
		/// </summary>
		public Task<List<StreamUserFollow>> Followers
		{
			get => Engine.Streamer.CurrentService?.GetUserFollowers( Id );
		}

		/// <summary>
		/// Ban user from your chat, the user will no longer be able to chat.
		/// Optionally specify the duration, a duration of zero means perm ban
		/// (Note: You have to be in your chat for this to work)
		/// </summary>
		public void Ban( string reason, int duration = 0 )
		{
			if ( duration == 0 )
			{
				Engine.Streamer.CurrentService?.BanUser( Login, reason );
			}
			else
			{
				Engine.Streamer.CurrentService?.TimeoutUser( Login, duration, reason );
			}
		}

		/// <summary>
		/// Unban user from your chat, this allows them to chat again
		/// (Note: You have to be in your chat for this to work)
		/// </summary>
		public void Unban()
		{
			Engine.Streamer.CurrentService?.UnbanUser( Login );
		}

		/// <summary>
		/// Create a clip of our stream, if we're streaming
		/// </summary>
		public Task<StreamClip> CreateClip( bool hasDelay = false )
		{
			return Engine.Streamer.CurrentService?.CreateClip( Id, hasDelay );
		}

		/// <summary>
		/// Start a poll on our channel with multiple choices, save the poll so you can end it later on
		/// </summary>
		public Task<StreamPoll> CreatePoll( string title, int duration, string[] choices )
		{
			return Engine.Streamer.CurrentService?.CreatePoll( Id, title, duration, choices );
		}

		/// <summary>
		/// Create a prediction on our channel to bet with channel points
		/// </summary>
		public Task<StreamPrediction> CreatePrediction( string title, int duration, string firstOutcome, string secondOutcome )
		{
			return Engine.Streamer.CurrentService?.CreatePrediction( Id, title, duration, firstOutcome, secondOutcome );
		}
	}
}
