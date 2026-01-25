namespace Sandbox.Obfuscation;

/// <summary>
/// Configuration options for the obfuscation pipeline.
/// </summary>
public class ObfuscationConfig
{
	/// <summary>
	/// Enable symbol renaming (classes, methods, fields, properties, etc.)
	/// </summary>
	public bool EnableSymbolRenaming { get; set; } = true;

	/// <summary>
	/// Enable string literal encryption
	/// </summary>
	public bool EnableStringEncryption { get; set; } = true;

	/// <summary>
	/// Enable metadata stripping (debug info, unnecessary attributes, etc.)
	/// </summary>
	public bool EnableMetadataStripping { get; set; } = true;

	/// <summary>
	/// Enable control flow obfuscation (transforms if/else, loops, etc.)
	/// </summary>
	public bool EnableControlFlowObfuscation { get; set; } = true;

	/// <summary>
	/// Enable anti-decompiler techniques
	/// </summary>
	public bool EnableAntiDecompiler { get; set; } = false;

	/// <summary>
	/// Seed for deterministic obfuscation. If null, uses random seed.
	/// Using the same seed produces the same obfuscated output.
	/// </summary>
	public int? Seed { get; set; }

	/// <summary>
	/// List of type name patterns to exclude from obfuscation (supports wildcards).
	/// Example: "MyNamespace.PublicApi.*"
	/// </summary>
	public List<string> ExcludeTypePatterns { get; set; } = new();

	/// <summary>
	/// List of member name patterns to exclude from obfuscation.
	/// Example: "On*" to exclude all methods starting with "On"
	/// </summary>
	public List<string> ExcludeMemberPatterns { get; set; } = new();

	/// <summary>
	/// Attributes that mark members to preserve (not obfuscate).
	/// These are in addition to the built-in s&box attributes.
	/// </summary>
	public List<string> PreserveAttributes { get; set; } = new();

	/// <summary>
	/// Creates a default configuration with all transforms enabled.
	/// </summary>
	public static ObfuscationConfig Default => new();

	/// <summary>
	/// Creates a minimal configuration with only symbol renaming.
	/// </summary>
	public static ObfuscationConfig Minimal => new()
	{
		EnableSymbolRenaming = true,
		EnableStringEncryption = false,
		EnableMetadataStripping = true,
		EnableControlFlowObfuscation = false,
		EnableAntiDecompiler = false
	};

	/// <summary>
	/// Creates a maximum protection configuration with all transforms.
	/// </summary>
	public static ObfuscationConfig Maximum => new()
	{
		EnableSymbolRenaming = true,
		EnableStringEncryption = true,
		EnableMetadataStripping = true,
		EnableControlFlowObfuscation = true,
		EnableAntiDecompiler = true
	};
}
