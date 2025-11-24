namespace Sandbox.Services;

/// <summary>
/// Package Reviews
/// </summary>
public sealed class Review
{
	public enum ReviewScore
	{
		None = 0,
		Negative = 1,
		Positive = 2,
		Promise = 3,
	}

	/// <summary>
	/// The player who made the review
	/// </summary>
	public Players.Profile Player { get; set; }

	/// <summary>
	/// The actual content (text only right now)
	/// </summary>
	public string Content { get; set; }

	/// <summary>
	/// The score of the review
	/// </summary>
	public ReviewScore Score { get; set; }

	/// <summary>
	/// How many seconds this user played
	/// </summary>
	public TimeSpan PlayTime { get; set; }

	/// <summary>
	/// Date this review was updated
	/// </summary>
	public DateTimeOffset Updated { get; set; }

	public static async Task<Review[]> Fetch( string packageIdent, int take = 50, int skip = 0 )
	{
		take = take.Clamp( 1, 50 );
		skip = skip.Clamp( 0, 5000 );

		try
		{
			var posts = await Sandbox.Backend.Package.GetReviews( packageIdent, skip, take );
			if ( posts is null || posts.Entries == null ) return Array.Empty<Review>();

			return posts.Entries.Select( From ).ToArray();
		}
		catch
		{
			return default;
		}
	}

	public static async Task<Review> Get( string packageIdent, SteamId steamid )
	{
		try
		{
			return From( await Sandbox.Backend.Package.GetReview( packageIdent, steamid ) );
		}
		catch
		{
			return default;
		}
	}

	internal static async Task Post( string packageIdent, ReviewScore score, string content )
	{
		//try
		//{
		await Sandbox.Backend.Package.PostReview( packageIdent, content, (int)score );
		//}
		//catch { }
	}

	internal static Review From( Sandbox.Services.PackageReviewDto p )
	{
		if ( p is null ) return default;

		return new Review
		{
			Player = Sandbox.Services.Players.Profile.From( p.Player ),
			Content = p.Content,
			Score = (ReviewScore)p.Score,
			PlayTime = TimeSpan.FromSeconds( p.SecondsPlayed ),
			Updated = p.Updated
		};
	}
}
