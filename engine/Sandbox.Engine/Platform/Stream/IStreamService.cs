using System.Threading.Tasks;
using System.Collections.Generic;

namespace Sandbox
{
	internal interface IStreamService
	{
		StreamService ServiceType { get; }

		Task<bool> Connect();
		void Disconnect();
		void SendMessage( string message );
		void ClearChat();

		void BanUser( string username, string reason );
		void UnbanUser( string username );
		void TimeoutUser( string username, int duration, string reason );

		void SetChannelGame( string gameId );
		void SetChannelLanguage( string language );
		void SetChannelTitle( string title );
		void SetChannelDelay( int delay );

		Task<StreamUser> GetUser( string username );
		Task<List<StreamUserFollow>> GetUserFollowing( string userId );
		Task<List<StreamUserFollow>> GetUserFollowers( string userId );
		Task<StreamChannel?> GetChannel();

		Task<StreamPoll> CreatePoll( string userId, string title, int duration, string[] choices );
		Task<StreamPoll> EndPoll( string userId, string pollId, bool archive = false );

		Task<StreamPrediction> CreatePrediction( string userId, string title, int duration, string firstOutcome, string secondOutcome );
		Task<StreamPrediction> LockPrediction( string userId, string predictionId );
		Task<StreamPrediction> CancelPrediction( string userId, string predictionId );
		Task<StreamPrediction> ResolvePrediction( string userId, string predictionId, string winningOutcomeId );

		Task<StreamClip> CreateClip( string userId, bool hasDelay );

		void Tick();
	}
}
