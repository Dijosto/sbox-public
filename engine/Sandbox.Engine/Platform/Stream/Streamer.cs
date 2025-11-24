namespace Sandbox;

public static class Streamer
{
	/// <summary>
	/// Your own username
	/// </summary>
	public static string Username => Engine.Streamer.Username;

	/// <summary>
	/// Your own user id
	/// </summary>
	public static string UserId => Engine.Streamer.UserId;

	/// <summary>
	/// The service type (ie "Twitch")
	/// </summary>
	public static StreamService Service => Engine.Streamer.ServiceType;

	/// <summary>
	/// Are we connected to a service
	/// </summary>
	public static bool IsActive => Engine.Streamer.IsActive;

	/// <summary>
	/// Get user information. If no username is specified, the user returned is ourself
	/// </summary>
	public static Task<StreamUser> GetUser( string username = null ) => Engine.Streamer.CurrentService?.GetUser( username );

	/// <summary>
	/// Get user following "Who is X following"
	/// </summary>
	internal static Task<List<StreamUserFollow>> GetUserFollowing( string userId ) => Engine.Streamer.CurrentService?.GetUserFollowing( userId );

	/// <summary>
	/// Get user followers "Who is following X"
	/// </summary>
	internal static Task<List<StreamUserFollow>> GetUserFollowers( string userId ) => Engine.Streamer.CurrentService?.GetUserFollowers( userId );

	/// <summary>
	/// Start a poll with choices, save the poll id so you can end it later on
	/// </summary>
	internal static Task<StreamPoll> CreatePoll( string userId, string title, int duration, string[] choices ) => Engine.Streamer.CurrentService?.CreatePoll( userId, title, duration, choices );

	/// <summary>
	/// End a poll using a saved poll id, you can optionally archive the poll or just terminate it
	/// </summary>
	internal static Task<StreamPoll> EndPoll( string userId, string pollId, bool archive = true ) => Engine.Streamer.CurrentService?.EndPoll( userId, pollId, archive );

	/// <summary>
	/// Create a prediction to bet with channel points
	/// </summary>
	internal static Task<StreamPrediction> CreatePrediction( string userId, string title, int duration, string firstOutcome, string secondOutcome )
		=> Engine.Streamer.CurrentService?.CreatePrediction( userId, title, duration, firstOutcome, secondOutcome );

	/// <summary>
	/// Lock a current prediction with prediction id
	/// </summary>
	internal static Task<StreamPrediction> LockPrediction( string userId, string predictionId )
	{
		return Engine.Streamer.CurrentService?.LockPrediction( userId, predictionId );
	}

	/// <summary>
	/// Cancel a current prediction with prediction id
	/// </summary>
	internal static Task<StreamPrediction> CancelPrediction( string userId, string predictionId )
	{
		return Engine.Streamer.CurrentService?.CancelPrediction( userId, predictionId );
	}

	/// <summary>
	/// Resolve a current prediction with prediction id and choose winning outcome to pay out channel points
	/// </summary>
	internal static Task<StreamPrediction> ResolvePrediction( string userId, string predictionId, string winningOutcomeId )
	{
		return Engine.Streamer.CurrentService?.ResolvePrediction( userId, predictionId, winningOutcomeId );
	}

	internal static Task<StreamClip> CreateClip( string userId, bool hasDelay = false )
	{
		return Engine.Streamer.CurrentService?.CreateClip( userId, hasDelay );
	}

	/// <summary>
	/// Send a message to chat, optionally specify channel you want to send the message, otherwise it is sent to your own chat
	/// </summary>
	public static void SendMessage( string message )
	{
		Engine.Streamer.CurrentService?.SendMessage( message );
	}

	/// <summary>
	/// Clear your own chat
	/// </summary>
	public static void ClearChat()
	{
		Engine.Streamer.CurrentService?.ClearChat();
	}

	/// <summary>
	/// Ban user from your chat by username, the user will no longer be able to chat.
	/// Optionally specify the duration, a duration of zero means perm ban
	/// (Note: You have to be in your chat for this to work)
	/// </summary>
	public static void BanUser( string username, string reason, int duration = 0 )
	{
		if ( duration == 0 )
		{
			Engine.Streamer.CurrentService?.BanUser( username, reason );
		}
		else
		{
			Engine.Streamer.CurrentService?.TimeoutUser( username, duration, reason );
		}
	}

	/// <summary>
	/// Unban user from your chat by username
	/// (Note: You have to be in your chat for this to work)
	/// </summary>
	public static void UnbanUser( string username )
	{
		Engine.Streamer.CurrentService?.UnbanUser( username );
	}

	/// <summary>
	/// Set the game you're playing by game id
	/// </summary>
	public static string Game
	{
		set => Engine.Streamer.CurrentService?.SetChannelGame( value );
	}

	/// <summary>
	/// Set the language of your stream
	/// </summary>
	public static string Language
	{
		set => Engine.Streamer.CurrentService?.SetChannelLanguage( value );
	}

	/// <summary>
	/// Set the title of your stream
	/// </summary>
	public static string Title
	{
		set => Engine.Streamer.CurrentService?.SetChannelTitle( value );
	}

	/// <summary>
	/// Set the delay of your stream
	/// </summary>
	public static int Delay
	{
		set
		{
			Engine.Streamer.CurrentService?.SetChannelDelay( value );
		}
	}

	/// <summary>
	/// Amount of concurrent viewer your stream has.
	/// </summary>
	public static int ViewerCount => Engine.Streamer.CurrentBroadcast.ViewerCount;
}
