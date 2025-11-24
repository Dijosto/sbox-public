namespace Sandbox.Protobuf;

public static class RatingMsg
{
	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class RatingAdded : IMessage
	{
		public static ushort MessageIdent => 2000;

		public Guid TargetGuid { get; set; }
		public long SteamId { get; set; }
		public int Rating { get; set; }
	}
}
