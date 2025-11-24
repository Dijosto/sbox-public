using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Facepunch.InteropGen;

public class Function
{
	// Cache for compiled regex - thread-safe and compiled for better performance
	private static readonly Regex _functionRegex = new(
		@"^(static)?[\s+]?(.+?)\s+([a-zA-Z0-9_]+?)\(((.+?))?\)( const)?;(.+)?",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	// Cache parsed function signatures to avoid re-parsing
	private static readonly ConcurrentDictionary<string, ParsedFunction> _parseCache = new();

	// Struct to hold parsed components efficiently
	private readonly struct ParsedFunction
	{
		public readonly bool IsStatic;
		public readonly string Name;
		public readonly string ReturnType;
		public readonly string Parameters;
		public readonly string Special;

		public ParsedFunction( bool isStatic, string name, string returnType, string parameters, string special )
		{
			IsStatic = isStatic;
			Name = name;
			ReturnType = returnType;
			Parameters = parameters;
			Special = special;
		}
	}

	public bool Native { get; set; }
	public bool Static { get; set; }
	public string Name { get; set; }
	public Arg Return { get; set; } = new ArgVoid();
	public Arg[] Parameters { get; set; } = [];
	public Class Class { get; set; }
	public string MangledName { get; set; }
	public List<string> Special { get; set; } = [];
	public StringBuilder Body { get; set; }

	public bool HasReturn => !Return.IsVoid;

	/// <summary>
	/// If set, then on the managed side we'll print this instead of the function
	/// </summary>
	public Func<string> ManagedCallReplacement { get; set; }
	public string NativeCallReplacement { get; set; }

	internal static Function Parse( string line )
	{
		string trimmedLine = line.Trim();

		// Check cache first
		if ( _parseCache.TryGetValue( trimmedLine, out ParsedFunction cached ) )
		{
			return CreateFunctionFromCached( cached );
		}

		// Parse with regex only if not cached
		Match match = _functionRegex.Match( trimmedLine );
		if ( !match.Success )
		{
			return null;
		}

		ParsedFunction parsed = new(
			isStatic: match.Groups[1].Success,
			name: match.Groups[3].Value.Trim(),
			returnType: match.Groups[2].Value,
			parameters: match.Groups[4].Value,
			special: match.Groups[7].Value
		);

		// Cache the result if cache isn't too large
		if ( _parseCache.Count < 1000 )
		{
			_ = _parseCache.TryAdd( trimmedLine, parsed );
		}

		return CreateFunctionFromCached( parsed );
	}

	private static Function CreateFunctionFromCached( ParsedFunction parsed )
	{
		Function f = new()
		{
			Native = true,
			Static = parsed.IsStatic,
			Name = parsed.Name,
			Return = Arg.Parse( parsed.ReturnType + " returnvalue" ),
			Parameters = Arg.ParseMany( parsed.Parameters )
		};

		f.AddSpecial( parsed.Special );
		return f;
	}

	internal void AddSpecial( string value )
	{
		if ( string.IsNullOrEmpty( value ) )
		{
			return;
		}

		// Use Span<char> for more efficient parsing on .NET 9
		ReadOnlySpan<char> span = value.AsSpan();

		// Split more efficiently without allocating intermediate strings
		int start = 0;
		for ( int i = 0; i <= span.Length; i++ )
		{
			if ( i == span.Length || span[i] == ' ' )
			{
				if ( i > start )
				{
					ReadOnlySpan<char> part = span[start..i];
					// Trim brackets more efficiently
					if ( part.Length > 0 && (part[0] == '[' || part[0] == ' ') )
					{
						part = part[1..];
					}

					if ( part.Length > 0 && (part[^1] == ']' || part[^1] == ' ') )
					{
						part = part[..^1];
					}

					if ( part.Length > 0 )
					{
						Special.Add( part.ToString() );
					}
				}
				start = i + 1;
			}
		}
	}

	internal string GetManagedName()
	{
		return Name == "GetType" ? "GetType_Native" : Name;
	}

	internal List<string> attr = [];

	internal void TakeAttributes( List<string> attributes )
	{
		attr.AddRange( attributes );
		attributes.Clear();
	}

	internal bool HasAttribute( string name )
	{
		return attr.Contains( name, StringComparer.OrdinalIgnoreCase ) || Class.HasAttribute( name );
	}

	/// <summary>
	/// [nogc] adds [SuppressGCTransition] to the function
	/// </summary>
	public bool IsNoGC
	{
		get
		{
			// we or our class are marked with nogc
			bool wantsNoGc = HasAttribute( "nogc" ) || Class.HasAttribute( "nogc" );
			if ( !wantsNoGc )
			{
				return false;
			}

			// can't do it if it's calling back to managed
			return !HasAttribute( "callback" );
		}
	}

	public Function Copy()
	{
		return new Function
		{
			Native = Native,
			Static = Static,
			Name = Name,
			Return = Return,
			Parameters = Parameters,
			Class = Class,
			MangledName = MangledName,
			Special = Special,
			attr = attr,
			ManagedCallReplacement = ManagedCallReplacement,
			NativeCallReplacement = NativeCallReplacement,
			Body = Body
		};
	}
}
