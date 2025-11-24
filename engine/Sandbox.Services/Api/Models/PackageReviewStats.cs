using System.Text.Json.Serialization;
namespace Sandbox.Services;

public class PackageReviewStats
{
	public struct Group
	{
		public int Rating { get; set; }

		public long Total { get; set; }
	}

	public List<Group> Scores { get; set; } = new();

	[JsonIgnore]
	public Group Positive => Scores.FirstOrDefault( x => x.Rating == (int)ReviewScore.Positive );

	[JsonIgnore]
	public Group Negative => Scores.FirstOrDefault( x => x.Rating == (int)ReviewScore.Negative );

	[JsonIgnore]
	public Group Promise => Scores.FirstOrDefault( x => x.Rating == (int)ReviewScore.Promise );

	[JsonIgnore]
	public long Count => Scores.Sum( x => x.Total );

	public float ToPercentage()
	{
		var count = Count;
		if ( count == 0 ) return 0;

		float score = (Positive.Total * 100) + (Promise.Total * 50);

		score /= (Positive.Total + Promise.Total + Negative.Total);

		return score;
	}
}
