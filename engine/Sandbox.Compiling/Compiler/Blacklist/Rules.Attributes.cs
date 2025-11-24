namespace Sandbox;

static partial class CompilerRules
{
	public static readonly List<string> Attributes =
	[
		"System.Runtime.CompilerServices.InlineArrayAttribute*",
		"System.Runtime.CompilerServices.ExtensionMarkerAttribute",
		"System.Runtime.CompilerServices.ParamCollectionAttribute"
	];
}
