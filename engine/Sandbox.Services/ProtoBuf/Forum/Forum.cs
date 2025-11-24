namespace Sandbox.Protobuf;

public static class ForumMsg
{
	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class ThreadPosted : IMessage
	{
		public static ushort MessageIdent => 7000;

		public long ForumId { get; set; }
		public long ThreadId { get; set; }
		public long PostId { get; set; }
		public long UserId { get; set; }
	}

	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class ReplyPosted : IMessage
	{
		public static ushort MessageIdent => 7001;

		public long ForumId { get; set; }
		public long ThreadId { get; set; }
		public long PostId { get; set; }
		public long UserId { get; set; }
	}

	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class ThreadEdited : IMessage
	{
		public static ushort MessageIdent => 7002;

		public long ForumId { get; set; }
		public long ThreadId { get; set; }
	}

}
