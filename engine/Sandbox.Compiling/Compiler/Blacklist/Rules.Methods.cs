namespace Sandbox;

static partial class CompilerRules
{
	public static readonly List<string> Methods =
	[
		"System.Runtime.CompilerServices.Unsafe.*",
		"System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan*",
	];
}
