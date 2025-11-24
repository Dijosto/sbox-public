namespace Sandbox.Protobuf;

public static class OrgMsg
{
	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class Created : IMessage
	{
		public static ushort MessageIdent => 8000;
		public string Ident { get; set; }
	}

	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class Edited : IMessage
	{
		public static ushort MessageIdent => 8001;

		public string Ident { get; set; }
	}
}
