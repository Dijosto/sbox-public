namespace Sandbox.Network;

[Expose]
struct TargetedMessage
{
	public Guid SenderId { get; set; }
	public Guid TargetId { get; set; }
	public object Message { get; set; }
	public byte Flags { get; set; }
}

[Expose]
struct TargetedInternalMessage
{
	public Guid SenderId { get; set; }
	public Guid TargetId { get; set; }
	public byte[] Data { get; set; }
	public byte Flags { get; set; }
}
