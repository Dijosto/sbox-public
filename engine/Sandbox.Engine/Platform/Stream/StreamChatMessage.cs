
namespace Sandbox
{
	public struct StreamChatMessage
	{
		public string Channel { get; internal set; }
		public string DisplayName { get; internal set; }
		public string Message { get; internal set; }
		public string Username { get; internal set; }
		public string Color { get; internal set; }
		public string[] Badges { get; internal set; }
	}
}
