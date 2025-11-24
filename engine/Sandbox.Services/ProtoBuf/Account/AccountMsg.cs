namespace Sandbox.Protobuf;

public static class AccountMsg
{
	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class Edited : IMessage
	{
		public static ushort MessageIdent => 9000;

		public long UserId { get; set; }
	}
}
