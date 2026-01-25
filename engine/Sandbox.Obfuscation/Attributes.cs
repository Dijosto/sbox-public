namespace Sandbox;

/// <summary>
/// Marks a type, method, property, or field to be excluded from obfuscation.
/// Use this attribute when you need to preserve specific names for reflection,
/// serialization, or external API compatibility.
/// </summary>
[AttributeUsage(
	AttributeTargets.Class |
	AttributeTargets.Struct |
	AttributeTargets.Interface |
	AttributeTargets.Enum |
	AttributeTargets.Method |
	AttributeTargets.Property |
	AttributeTargets.Field |
	AttributeTargets.Event |
	AttributeTargets.Constructor,
	AllowMultiple = false,
	Inherited = true )]
public sealed class DontObfuscateAttribute : Attribute
{
}

/// <summary>
/// Marks string literals in a method to be excluded from string encryption.
/// Useful for strings that must remain readable (like format strings for external APIs).
/// </summary>
[AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false )]
public sealed class DontEncryptStringsAttribute : Attribute
{
}

/// <summary>
/// Marks a method to be excluded from control flow obfuscation.
/// Use when performance-critical code would be impacted.
/// </summary>
[AttributeUsage( AttributeTargets.Method, AllowMultiple = false )]
public sealed class DontObfuscateControlFlowAttribute : Attribute
{
}
