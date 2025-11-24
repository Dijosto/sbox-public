
namespace Sandbox;

partial class PartyRoom
{
	public Action<Friend, string> OnChatMessage { get; set; }
	public Action<Friend> OnJoin { get; set; }
	public Action<Friend> OnLeave { get; set; }
	public Action<Friend, byte[]> OnVoiceData { get; set; }


}
