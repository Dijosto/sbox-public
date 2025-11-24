using System;

namespace Sandbox
{
	public struct StreamUserFollow
	{
		public string UserId { get; internal set; }
		public string Username { get; internal set; }
		public string DisplayName { get; internal set; }
		public DateTimeOffset CreatedAt { get; internal set; }
	}
}
