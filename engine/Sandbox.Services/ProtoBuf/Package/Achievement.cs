namespace Sandbox.Protobuf;

public static partial class AchievementMsg
{

	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class AchievementUnlocked : IMessage
	{
		public static ushort MessageIdent => 6001;

		public string Title { get; set; }
		public string Description { get; set; }
		public string Icon { get; set; }
		public int ScoreAdded { get; set; }
		public int PackageScore { get; set; }
		public int PlayerScore { get; set; }
		public int PackageUnlocks { get; set; }
		public int PlayerUnlocks { get; set; }
	}

}
