using Mono.Cecil;

namespace Sandbox.Obfuscation;

/// <summary>
/// Analyzes an assembly to determine which types and members should NOT be obfuscated.
/// This preserves s&box engine functionality, serialization, networking, and public APIs.
/// </summary>
public class PreservationAnalyzer
{
	private readonly ObfuscationConfig _config;
	private readonly HashSet<string> _preservedTypes = new();
	private readonly HashSet<string> _preservedMembers = new();

	/// <summary>
	/// Attributes that indicate a member must be preserved for s&box functionality.
	/// </summary>
	private static readonly HashSet<string> SboxPreserveAttributes = new( StringComparer.OrdinalIgnoreCase )
	{
		// Core s&box attributes
		"PropertyAttribute",
		"EventAttribute",
		"ButtonAttribute",
		"LibraryAttribute",
		"GameResourceAttribute",
		"IconAttribute",
		"TitleAttribute",
		"DescriptionAttribute",
		"GroupAttribute",
		"CategoryAttribute",
		"OrderAttribute",

		// Networking attributes
		"SyncAttribute",
		"HostSyncAttribute",
		"BroadcastAttribute",
		"AuthorityAttribute",
		"RpcAttribute",
		"Rpc+BroadcastAttribute",
		"Rpc+AuthorityAttribute",

		// Console commands/vars
		"ConVarAttribute",
		"ConCmdAttribute",

		// Serialization
		"JsonIncludeAttribute",
		"JsonPropertyNameAttribute",
		"JsonIgnoreAttribute",

		// UI/Razor
		"ParameterAttribute",
		"CascadingParameterAttribute",
		"StyleSheetAttribute",

		// Inspector visibility
		"ShowIfAttribute",
		"HideIfAttribute",
		"EnableIfAttribute",
		"DisableIfAttribute",
		"RequireComponentAttribute",

		// Action graphs
		"ActionGraphNodeAttribute",
		"ActionGraphPropertyAttribute",
		"ActionGraphTargetAttribute",
		"PureAttribute",
		"ImpureAttribute",
		"ExpressionNodeAttribute",
		"ActionNodeAttribute",

		// Our own attribute
		"DontObfuscateAttribute",
		"DontEncryptStringsAttribute",
		"DontObfuscateControlFlowAttribute",

		// System serialization
		"SerializableAttribute",
		"DataContractAttribute",
		"DataMemberAttribute",
	};

	/// <summary>
	/// Base types that indicate a type should be preserved (at least partially).
	/// </summary>
	private static readonly HashSet<string> SboxBaseTypes = new( StringComparer.OrdinalIgnoreCase )
	{
		"Sandbox.Component",
		"Sandbox.GameResource",
		"Sandbox.Panel",
		"Sandbox.PanelComponent",
		"Sandbox.BaseComponent",
	};

	/// <summary>
	/// Interface patterns that indicate members should be preserved.
	/// </summary>
	private static readonly HashSet<string> PreserveInterfaces = new( StringComparer.OrdinalIgnoreCase )
	{
		"IValid",
		"INetworkSerializable",
		"IComponent",
	};

	public PreservationAnalyzer( ObfuscationConfig config )
	{
		_config = config;
	}

	/// <summary>
	/// Analyze the assembly and build preservation sets.
	/// </summary>
	public PreservationResult Analyze( AssemblyDefinition assembly )
	{
		var result = new PreservationResult();

		foreach ( var module in assembly.Modules )
		{
			foreach ( var type in module.GetTypes() )
			{
				AnalyzeType( type, result );
			}
		}

		return result;
	}

	private void AnalyzeType( TypeDefinition type, PreservationResult result )
	{
		// Check if entire type should be preserved
		if ( ShouldPreserveType( type ) )
		{
			result.PreservedTypes.Add( type.FullName );
		}

		// Check if type matches exclusion patterns
		if ( MatchesExcludePattern( type.FullName, _config.ExcludeTypePatterns ) )
		{
			result.PreservedTypes.Add( type.FullName );
		}

		// Analyze members
		foreach ( var method in type.Methods )
		{
			if ( ShouldPreserveMember( method, type ) )
			{
				result.PreservedMembers.Add( GetMemberKey( method ) );
			}

			// Check for string encryption exclusion
			if ( HasAttribute( method, "DontEncryptStringsAttribute" ) )
			{
				result.NoStringEncryption.Add( GetMemberKey( method ) );
			}

			// Check for control flow exclusion
			if ( HasAttribute( method, "DontObfuscateControlFlowAttribute" ) )
			{
				result.NoControlFlowObfuscation.Add( GetMemberKey( method ) );
			}
		}

		foreach ( var property in type.Properties )
		{
			if ( ShouldPreserveProperty( property, type ) )
			{
				result.PreservedMembers.Add( GetMemberKey( property ) );

				// Also preserve getter/setter
				if ( property.GetMethod != null )
					result.PreservedMembers.Add( GetMemberKey( property.GetMethod ) );
				if ( property.SetMethod != null )
					result.PreservedMembers.Add( GetMemberKey( property.SetMethod ) );
			}
		}

		foreach ( var field in type.Fields )
		{
			if ( ShouldPreserveField( field, type ) )
			{
				result.PreservedMembers.Add( GetMemberKey( field ) );
			}
		}

		foreach ( var evt in type.Events )
		{
			if ( ShouldPreserveEvent( evt, type ) )
			{
				result.PreservedMembers.Add( GetMemberKey( evt ) );
			}
		}

		// Analyze nested types
		foreach ( var nested in type.NestedTypes )
		{
			AnalyzeType( nested, result );
		}
	}

	private bool ShouldPreserveType( TypeDefinition type )
	{
		// Always preserve public types (they're part of the API)
		if ( type.IsPublic || type.IsNestedPublic )
			return true;

		// Check for DontObfuscate attribute
		if ( HasAttribute( type, "DontObfuscateAttribute" ) )
			return true;

		// Check if inherits from s&box base types
		if ( InheritsFromSboxType( type ) )
			return true;

		// Check if implements preserved interfaces
		if ( ImplementsPreservedInterface( type ) )
			return true;

		// Check for s&box attributes on the type
		if ( HasAnySboxAttribute( type ) )
			return true;

		return false;
	}

	private bool ShouldPreserveMember( MethodDefinition method, TypeDefinition declaringType )
	{
		// Preserve public members
		if ( method.IsPublic )
			return true;

		// Preserve constructors (needed for reflection/serialization)
		if ( method.IsConstructor )
			return true;

		// Preserve virtual methods (might be overridden)
		if ( method.IsVirtual || method.IsAbstract )
			return true;

		// Check for DontObfuscate
		if ( HasAttribute( method, "DontObfuscateAttribute" ) )
			return true;

		// Check for s&box attributes
		if ( HasAnySboxAttribute( method ) )
			return true;

		// Check member name patterns
		if ( MatchesExcludePattern( method.Name, _config.ExcludeMemberPatterns ) )
			return true;

		// Preserve event handlers (On* methods in Components)
		if ( InheritsFromSboxType( declaringType ) && method.Name.StartsWith( "On" ) )
			return true;

		return false;
	}

	private bool ShouldPreserveProperty( PropertyDefinition property, TypeDefinition declaringType )
	{
		// Check visibility via getter/setter
		var isPublic = ( property.GetMethod?.IsPublic ?? false ) || ( property.SetMethod?.IsPublic ?? false );
		if ( isPublic )
			return true;

		// Check for DontObfuscate
		if ( HasAttribute( property, "DontObfuscateAttribute" ) )
			return true;

		// Check for s&box attributes (Property, Sync, etc.)
		if ( HasAnySboxAttribute( property ) )
			return true;

		// Check patterns
		if ( MatchesExcludePattern( property.Name, _config.ExcludeMemberPatterns ) )
			return true;

		return false;
	}

	private bool ShouldPreserveField( FieldDefinition field, TypeDefinition declaringType )
	{
		if ( field.IsPublic )
			return true;

		if ( HasAttribute( field, "DontObfuscateAttribute" ) )
			return true;

		if ( HasAnySboxAttribute( field ) )
			return true;

		// Preserve backing fields for preserved properties
		if ( field.Name.Contains( "BackingField" ) )
			return true;

		if ( MatchesExcludePattern( field.Name, _config.ExcludeMemberPatterns ) )
			return true;

		return false;
	}

	private bool ShouldPreserveEvent( EventDefinition evt, TypeDefinition declaringType )
	{
		var isPublic = ( evt.AddMethod?.IsPublic ?? false ) || ( evt.RemoveMethod?.IsPublic ?? false );
		if ( isPublic )
			return true;

		if ( HasAttribute( evt, "DontObfuscateAttribute" ) )
			return true;

		if ( HasAnySboxAttribute( evt ) )
			return true;

		return false;
	}

	private bool InheritsFromSboxType( TypeDefinition type )
	{
		var current = type.BaseType;
		while ( current != null )
		{
			if ( SboxBaseTypes.Contains( current.FullName ) )
				return true;

			try
			{
				var resolved = current.Resolve();
				current = resolved?.BaseType;
			}
			catch
			{
				break;
			}
		}
		return false;
	}

	private bool ImplementsPreservedInterface( TypeDefinition type )
	{
		foreach ( var iface in type.Interfaces )
		{
			var ifaceName = iface.InterfaceType.Name;
			if ( PreserveInterfaces.Any( p => ifaceName.Contains( p ) ) )
				return true;
		}
		return false;
	}

	private bool HasAnySboxAttribute( ICustomAttributeProvider provider )
	{
		if ( !provider.HasCustomAttributes )
			return false;

		foreach ( var attr in provider.CustomAttributes )
		{
			var attrName = attr.AttributeType.Name;
			if ( SboxPreserveAttributes.Contains( attrName ) )
				return true;

			// Check custom preserve attributes from config
			if ( _config.PreserveAttributes.Contains( attrName ) )
				return true;
		}
		return false;
	}

	private bool HasAttribute( ICustomAttributeProvider provider, string attributeName )
	{
		if ( !provider.HasCustomAttributes )
			return false;

		return provider.CustomAttributes.Any( a => a.AttributeType.Name == attributeName );
	}

	private bool MatchesExcludePattern( string name, List<string> patterns )
	{
		foreach ( var pattern in patterns )
		{
			if ( MatchesWildcard( name, pattern ) )
				return true;
		}
		return false;
	}

	private static bool MatchesWildcard( string input, string pattern )
	{
		if ( string.IsNullOrEmpty( pattern ) )
			return false;

		// Simple wildcard matching (* only)
		if ( pattern == "*" )
			return true;

		if ( pattern.StartsWith( "*" ) && pattern.EndsWith( "*" ) )
		{
			var middle = pattern[1..^1];
			return input.Contains( middle, StringComparison.OrdinalIgnoreCase );
		}

		if ( pattern.StartsWith( "*" ) )
		{
			var suffix = pattern[1..];
			return input.EndsWith( suffix, StringComparison.OrdinalIgnoreCase );
		}

		if ( pattern.EndsWith( "*" ) )
		{
			var prefix = pattern[..^1];
			return input.StartsWith( prefix, StringComparison.OrdinalIgnoreCase );
		}

		return string.Equals( input, pattern, StringComparison.OrdinalIgnoreCase );
	}

	private static string GetMemberKey( MemberReference member )
	{
		return $"{member.DeclaringType.FullName}::{member.Name}";
	}
}

/// <summary>
/// Result of preservation analysis containing sets of items that should not be obfuscated.
/// </summary>
public class PreservationResult
{
	/// <summary>
	/// Type full names that should not be renamed.
	/// </summary>
	public HashSet<string> PreservedTypes { get; } = new();

	/// <summary>
	/// Member keys (Type::Member) that should not be renamed.
	/// </summary>
	public HashSet<string> PreservedMembers { get; } = new();

	/// <summary>
	/// Members that should not have string encryption applied.
	/// </summary>
	public HashSet<string> NoStringEncryption { get; } = new();

	/// <summary>
	/// Members that should not have control flow obfuscation applied.
	/// </summary>
	public HashSet<string> NoControlFlowObfuscation { get; } = new();

	/// <summary>
	/// Check if a type should be preserved.
	/// </summary>
	public bool IsTypePreserved( TypeDefinition type ) => PreservedTypes.Contains( type.FullName );

	/// <summary>
	/// Check if a member should be preserved.
	/// </summary>
	public bool IsMemberPreserved( MemberReference member )
	{
		var key = $"{member.DeclaringType.FullName}::{member.Name}";
		return PreservedMembers.Contains( key );
	}
}
