namespace Sandbox.Twitch
{
	internal class TwitchService : IStreamService
	{
		private readonly TwitchClient _client;
		private readonly TwitchAPI _api;

		public StreamService ServiceType => StreamService.Twitch;

		public TwitchService()
		{
			_client = new TwitchClient();
			_api = new TwitchAPI();
		}

		public async Task<StreamUser> GetUser( string username )
		{
			var user = await _api.GetUser( username );

			return new StreamUser
			{
				Id = user.Id,
				Login = user.Login,
				DisplayName = user.DisplayName,
				UserType = user.UserType,
				BroadcasterType = user.BroadcasterType,
				Description = user.Description,
				ProfileImageUrl = user.ProfileImageUrl,
				OfflineImageUrl = user.OfflineImageUrl,
				ViewCount = user.ViewCount,
				Email = user.Email,
				CreatedAt = DateTimeOffset.Parse( user.CreatedAt ),
			};
		}

		public async Task<List<StreamUserFollow>> GetUserFollowing( string userId )
		{
			var follows = await _api.GetUserFollowing( userId );

			return follows.UserFollows.Select( follow => (StreamUserFollow)new StreamUserFollow
			{
				UserId = follow.ToId,
				Username = follow.ToLogin,
				DisplayName = follow.ToName,
				CreatedAt = DateTimeOffset.Parse( follow.FollowedAt ),
			} ).ToList();
		}

		public async Task<List<StreamUserFollow>> GetUserFollowers( string userId )
		{
			var follows = await _api.GetUserFollowers( userId );

			return follows.UserFollows.Select( follow => (StreamUserFollow)new StreamUserFollow
			{
				UserId = follow.FromId,
				Username = follow.FromLogin,
				DisplayName = follow.FromName,
				CreatedAt = DateTimeOffset.Parse( follow.FollowedAt ),
			} ).ToList();
		}

		public async Task<StreamChannel?> GetChannel()
		{
			var channel = await _api.GetChannel( _client.Username );
			if ( channel == null )
				return null;

			return new StreamChannel
			{
				UserId = channel.BroadcasterId,
				Username = channel.BroadcasterLogin,
				DisplayName = channel.BroadcasterName,
				Language = channel.BroadcasterLanguage,
				GameId = channel.GameId,
				GameName = channel.GameName,
				Title = channel.Title,
				Delay = channel.Delay,
			};
		}


		public async Task<StreamPoll> CreatePoll( string userId, string title, int duration, string[] choices )
		{
			var poll = await _api.CreatePoll( userId, title, duration, choices );

			return new StreamPoll( poll );
		}

		public async Task<StreamPoll> EndPoll( string userId, string pollId, bool archive )
		{
			var poll = await _api.EndPoll( userId, pollId, archive );

			return new StreamPoll( poll );
		}

		public async Task<StreamPrediction> CreatePrediction( string userId, string title, int duration, string firstOutcome, string secondOutcome )
		{
			var prediction = await _api.CreatePrediction( userId, title, duration, firstOutcome, secondOutcome );

			return new StreamPrediction( prediction );
		}

		public async Task<StreamPrediction> LockPrediction( string userId, string predictionId )
		{
			var prediction = await _api.LockPrediction( userId, predictionId );

			return new StreamPrediction( prediction );
		}

		public async Task<StreamPrediction> CancelPrediction( string userId, string predictionId )
		{
			var prediction = await _api.CancelPrediction( userId, predictionId );

			return new StreamPrediction( prediction );
		}

		public async Task<StreamPrediction> ResolvePrediction( string userId, string predictionId, string winningOutcomeId )
		{
			var prediction = await _api.ResolvePrediction( userId, predictionId, winningOutcomeId );

			return new StreamPrediction( prediction );
		}

		public async Task<StreamClip> CreateClip( string userId, bool hasDelay )
		{
			var clip = await _api.CreateClip( userId, hasDelay );

			return new StreamClip( clip );
		}

		public void BanUser( string username, string reason )
		{
			_client.BanUser( username, reason );
		}

		public void ClearChat()
		{
			_client.ClearChat();
		}

		public async Task<bool> Connect()
		{
			return await _client.Connect();
		}

		public void Disconnect()
		{
			_client.Disconnect();
		}

		public void JoinChannel( string channel )
		{
			_client.JoinChannel( channel );
		}

		public void LeaveChannel( string channel )
		{
			_client.LeaveChannel( channel );
		}

		public void SendMessage( string message )
		{
			_client.SendMessage( message );
		}

		public void SetChannelDelay( int delay )
		{
			_api.SetChannelDelay( Engine.Streamer.UserId, delay );
		}

		public void SetChannelGame( string gameId )
		{
			_api.SetChannelGame( Engine.Streamer.UserId, gameId );
		}

		public void SetChannelLanguage( string language )
		{
			_api.SetChannelLanguage( Engine.Streamer.UserId, language );
		}

		public void SetChannelTitle( string title )
		{
			_api.SetChannelTitle( Engine.Streamer.UserId, title );
		}

		public void TimeoutUser( string username, int duration, string reason )
		{
			_client.TimeoutUser( username, duration, reason );
		}

		public void UnbanUser( string username )
		{
			_client.UnbanUser( username );
		}

		RealTimeUntil timeUntilUpdateBroadcast;


		void IStreamService.Tick()
		{
			if ( timeUntilUpdateBroadcast <= 0 )
			{
				timeUntilUpdateBroadcast = 30; // Update again in 30 seconds
				UpdateBroadcast();
			}
		}

		async void UpdateBroadcast()
		{
			var viewers = Engine.Streamer.CurrentBroadcast.ViewerCount;

			var broadcast = await _api.GetStream( _client.UserId );

			if ( broadcast != null )
			{
				Engine.Streamer.CurrentBroadcast = new StreamBroadcast( broadcast );

				// Things are changing, update more often
				if ( viewers != Engine.Streamer.CurrentBroadcast.ViewerCount )
				{
					timeUntilUpdateBroadcast = 10;
				}

			}
			else
			{
				Engine.Streamer.CurrentBroadcast = default;
			}
		}
	}
}
