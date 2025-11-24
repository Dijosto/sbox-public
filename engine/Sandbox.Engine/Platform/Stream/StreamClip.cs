using Sandbox.Twitch;

namespace Sandbox
{
	public struct StreamClip
	{
		internal StreamClip( TwitchAPI.CreateClipResponse clip )
		{
			EditUrl = clip.EditUrl;
			Id = clip.Id;
		}

		public string EditUrl { get; internal set; }
		public string Id { get; internal set; }
	}
}
