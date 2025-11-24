namespace Sandbox.Services;

public enum ReviewScore
{
	None = 0,
	Negative = 1,
	Positive = 2,
	Promise = 3,
}

public class PackageReviewList
{
	public int Count { get; set; }
	public int Skip { get; set; }
	public int Take { get; set; }
	public List<PackageReviewDto> Entries { get; set; } = new();
}

public class PackageReviewDto
{
	/// <summary>
	/// The player that made the review
	/// </summary>
	public Player Player { get; set; }

	/// <summary>
	/// The actual content
	/// </summary>
	public string Content { get; set; }

	/// <summary>
	/// The score of the review
	/// </summary>
	public ReviewScore Score { get; set; }

	/// <summary>
	/// The package being reviewed
	/// </summary>
	public PackageWrapMinimal Package { get; set; }

	/// <summary>
	/// How many seconds this user played
	/// </summary>
	public int SecondsPlayed { get; set; }

	/// <summary>
	/// When it was created
	/// </summary>
	public DateTimeOffset Created { get; set; }

	/// <summary>
	/// When it was updated
	/// </summary>
	public DateTimeOffset Updated { get; set; }
}
