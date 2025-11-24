using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Facepunch.InteropGen;

public class Arg
{
	// Static cache for type mappings - thread-safe
	private static readonly ConcurrentDictionary<string, Type> _typeCache = new( StringComparer.OrdinalIgnoreCase );

	// Thread-safe cache for parsed string operations
	private static readonly ConcurrentDictionary<string, (string type, string name, string[] flags, Type wrapper)> _parseCache
		= new();

	// Pre-compiled separators for faster parsing
	private static readonly char[] _spaceSeparator = [' '];
	private static readonly char[] _commaSeparator = [','];

	// Static constructor to initialize the cache once
	static Arg()
	{
		InitializeTypeCache();
	}

	private static void InitializeTypeCache()
	{
		foreach ( Type t in typeof( Arg ).Assembly.GetTypes().Where( x => typeof( Arg ).IsAssignableFrom( x ) ) )
		{
			foreach ( TypeNameAttribute a in t.GetCustomAttributes( typeof( TypeNameAttribute ), false ) )
			{
				_ = _typeCache.TryAdd( a.TypeName, t );
			}
		}
	}

	public bool IsSelf { get; set; }
	public virtual string Name { get; set; }
	public Type Wrapper { get; private set; }
	public string[] Flags { get; set; }

	public bool HasFlag( string flag )
	{
		return Flags != null && Flags.Contains( flag );
	}

	public virtual string ManagedType => "!!UNKNOWN!!";
	public virtual string NativeType => ManagedType;
	public virtual string ManagedDelegateType => ManagedType;
	public virtual string NativeDelegateType => NativeType;
	public virtual bool IsVoid => false;
	public virtual string DefaultValue => "0";
	public bool IsReturn => Name == "returnvalue";

	public virtual string GetManagedDelegateType( bool incoming )
	{
		return ManagedDelegateType;
	}

	public virtual string GetNativeDelegateType( bool incoming )
	{
		return NativeDelegateType;
	}

	/// <summary>
	/// If set to false it won't be provided as an argument.
	/// This is used for things like passing literals
	/// </summary>
	public virtual bool IsRealArgument => true;

	public virtual string ToInterop( bool native, string code = null )
	{
		code ??= Name;
		return code;
	}

	public virtual string FromInterop( bool native, string code = null )
	{
		code ??= Name;
		return code;
	}

	internal static Arg Parse( string line )
	{
		string trimmedLine = line.Trim();

		// Check parse cache first - thread-safe
		if ( _parseCache.TryGetValue( trimmedLine, out (string type, string name, string[] flags, Type wrapper) cached ) )
		{
			return CreateArgFromCached( cached );
		}

		(string type, string name, string[] flags, Type wrapper) result = ParseInternal( trimmedLine );

		// Cache the result with size limit - thread-safe
		if ( _parseCache.Count < 1000 )
		{
			_ = _parseCache.TryAdd( trimmedLine, result );
		}

		return CreateArgFromCached( result );
	}

	private static (string type, string name, string[] flags, Type wrapper) ParseInternal( string type )
	{
		string name = "none";

		if ( type.StartsWith( '[' ) && type.EndsWith( ']' ) )
		{
			// Special case for literals - return early
			return ("literal", type[1..^1], null, null);
		}

		int lastSpaceIndex = type.LastIndexOf( ' ' );
		if ( lastSpaceIndex > 0 )
		{
			name = type[(lastSpaceIndex + 1)..];
			type = type[..lastSpaceIndex];
		}

		if ( type == "const" )
		{
			throw new System.Exception( "Invalid argument" );
		}

		Type wrapper = null;
		string[] flags = null;

		// Parse flags more efficiently
		if ( type.Contains( ' ' ) )
		{
			int lastSpace = type.LastIndexOf( ' ' );
			string flagsString = type[..lastSpace];
			flags = flagsString.Split( _spaceSeparator, StringSplitOptions.RemoveEmptyEntries );
			type = type[(lastSpace + 1)..];
		}

		// Check for array wrapper
		if ( type.EndsWith( "[]" ) )
		{
			type = type[..^2];
			wrapper = typeof( ArgArray );
		}

		return (type, name, flags, wrapper);
	}

	private static Arg CreateArgFromCached( (string type, string name, string[] flags, Type wrapper) cached )
	{
		// Handle literal case
		if ( cached.type == "literal" )
		{
			return new ArgLiteral( cached.name );
		}

		string normalizedType = cached.type.ToLowerInvariant();

		// Use cached lookup instead of reflection - thread-safe
		if ( _typeCache.TryGetValue( normalizedType, out Type argType ) )
		{
			Arg arg = Activator.CreateInstance( argType ) as Arg;
			arg.Name = cached.name;
			arg.Wrapper = cached.wrapper;
			arg.Flags = cached.flags;

			return arg.Wrap( arg );
		}

		return new ArgUnknown
		{
			Type = cached.type,
			Name = cached.name,
			Wrapper = cached.wrapper,
			Flags = cached.flags
		};
	}

	//
	// funccall( args, args, args ) to
	// returnvalue = funccall( args, args, args );
	//
	public virtual string ReturnWrapCall( string functionCall, bool native )
	{
		return $"return {functionCall};";
	}

	internal static Arg[] ParseMany( string value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
		{
			return Array.Empty<Arg>();
		}

		// Use pre-allocated separator array and StringSplitOptions for better performance
		string[] parts = value.Split( _commaSeparator, StringSplitOptions.RemoveEmptyEntries );
		Arg[] result = new Arg[parts.Length];

		for ( int i = 0; i < parts.Length; i++ )
		{
			result[i] = Parse( parts[i].Trim() );
		}

		return result;
	}

	public virtual string WrapFunctionCall( string functionCall, bool native )
	{
		return functionCall;
	}

	/// <summary>
	/// If the unknown type is an array etc, we need to wrap it with an array type
	/// </summary>
	public Arg Wrap( Arg arg )
	{
		if ( Wrapper == null )
		{
			return new ArgFlagsWrapper( arg );
		}

		Arg a = Activator.CreateInstance( Wrapper, arg ) as Arg;

		return a;
	}
}
