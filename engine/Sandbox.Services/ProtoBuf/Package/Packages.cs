namespace Sandbox.Protobuf;

public static class PackageMsg
{
	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class UsageChanged : IMessage
	{
		public static ushort MessageIdent => 5000;

		public string PackageIdent { get; set; }
		public long UserCount { get; set; }
	}

	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class FavouritesChanged : IMessage
	{
		public static ushort MessageIdent => 5001;

		public string PackageIdent { get; set; }
		public long Value { get; set; }
	}

	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class VotesChanged : IMessage
	{
		public static ushort MessageIdent => 5002;

		public string PackageIdent { get; set; }
		public long VotesUp { get; set; }
		public long VotesDown { get; set; }
	}

	/// <summary>
	/// The package name or description or something was updated
	/// </summary>
	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class Changed : IMessage
	{
		public static ushort MessageIdent => 5003;

		public string PackageIdent { get; set; }
	}

	/// <summary>
	/// The package hasd a new version
	/// </summary>
	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class Update : IMessage
	{
		public static ushort MessageIdent => 5004;

		public string PackageIdent { get; set; }
		public long RevisionId { get; set; }
	}

	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class ViewsChanged : IMessage
	{
		public static ushort MessageIdent => 5005;

		public string PackageIdent { get; set; }
		public long Value { get; set; }
	}


	[ProtoContract( ImplicitFields = ImplicitFields.AllFields )]
	public class ReviewPosted : IMessage
	{
		public static ushort MessageIdent => 5006;

		public string PackageIdent { get; set; }
		public long Score { get; set; }
		public long SteamId { get; set; }
		public string DisplayName { get; set; }
	}

}
