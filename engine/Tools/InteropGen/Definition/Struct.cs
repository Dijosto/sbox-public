using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Facepunch.InteropGen;

public class Struct
{
	// Precompiled regex for better performance
	private static readonly Regex _structParseRegex = new(
		@"([\w.:\(\)]+)( [\s+]?(?:as|is) [\s+]?([\w.:]+))?(.+)?",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	// Cache for struct parsing results
	private static readonly ConcurrentDictionary<string, ParsedStructDefinition> _structParseCache = new();

	// Struct to hold parsed components efficiently
	private readonly struct ParsedStructDefinition
	{
		public readonly string Name;
		public readonly string Alias;
		public readonly string ExtraInfo;

		public ParsedStructDefinition( string name, string alias, string extraInfo )
		{
			Name = name;
			Alias = alias;
			ExtraInfo = extraInfo;
		}
	}

	public string NativeName { get; set; }
	public string NativeNamespace { get; set; }

	public string ManagedName { get; set; }
	public string ManagedNamespace { get; set; }

	public bool IsEnum { get; set; }

	/// <summary>
	/// A native type that is a pointer but is wrapped in a way where it pretends it isn't one
	/// Usually wrapped using DECLARE_POINTER_HANDLE etc
	/// </summary>
	public bool IsPointer { get; set; }

	public string CreateUsing { get; set; }

	private string _nativeNameWithNamespace;
	public string NativeNameWithNamespace
	{
		get
		{
			if ( _nativeNameWithNamespace is not null )
			{
				return _nativeNameWithNamespace;
			}

			if ( string.IsNullOrEmpty( NativeNamespace ) )
			{
				return NativeName;
			}

			_nativeNameWithNamespace = $"{NativeNamespace}::{NativeName}";
			return _nativeNameWithNamespace;
		}
	}

	private string _managedNameWithNamespace;
	public string ManagedNameWithNamespace
	{
		get
		{
			if ( _managedNameWithNamespace is not null )
			{
				return _managedNameWithNamespace;
			}

			if ( string.IsNullOrEmpty( ManagedNamespace ) )
			{
				return ManagedName;
			}

			_managedNameWithNamespace = $"{ManagedNamespace}.{ManagedName}";
			return _managedNameWithNamespace;
		}
	}

	internal static Struct Parse( bool isNative, string type, string line )
	{
		// Check cache first
		if ( _structParseCache.TryGetValue( line, out ParsedStructDefinition cached ) )
		{
			return CreateFromCached( isNative, type, cached );
		}

		// Parse with regex only if not cached
		Match match = _structParseRegex.Match( line );
		if ( !match.Success )
		{
			Log.WriteLine( $"Couldn't parse {type} definition: {line}" );
			return null;
		}

		ParsedStructDefinition parsed = new(
			name: match.Groups[1].Value,
			alias: match.Groups[3].Value,
			extraInfo: match.Groups[4].Value
		);

		// Cache the result if cache isn't too large
		if ( _structParseCache.Count < 1000 )
		{
			_structParseCache.TryAdd( line, parsed );
		}

		return CreateFromCached( isNative, type, parsed );
	}

	private static Struct CreateFromCached( bool isNative, string type, ParsedStructDefinition parsed )
	{
		string name = parsed.Name;
		string alias = parsed.Alias;

		if ( string.IsNullOrWhiteSpace( alias ) )
		{
			alias = name;
		}

		if ( !isNative )
		{
			(name, alias) = (alias, name);
		}

		Struct s = new()
		{
			NativeName = name,
			ManagedName = alias
		};

		if ( name.Contains( '.' ) )
		{
			int last = name.LastIndexOf( '.' );
			s.NativeName = name[(last + 1)..];
			s.NativeNamespace = name[..last].Replace( ".", "::" );
		}

		if ( alias.Contains( '.' ) )
		{
			int last = alias.LastIndexOf( '.' );
			s.ManagedName = alias[(last + 1)..];
			s.ManagedNamespace = alias[..last];
		}

		s.IsEnum = type == "enum";
		s.IsPointer = type == "pointer";

		s.ParseExtra( parsed.ExtraInfo );

		return s;
	}

	private void ParseExtra( string value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
		{
			return;
		}

		value = value.Trim( ' ', '[', ']', ';' );

		string[] flags = value.Split( ';' );

		foreach ( string flag in flags )
		{
			if ( flag.StartsWith( "CreateUsing:" ) )
			{
				CreateUsing = flag.Replace( "CreateUsing:", "" );
				CreateUsing = CreateUsing.Replace( "self.", $"{NativeNameWithNamespace}::" );
			}
		}

	}

	private readonly List<string> attr = [];

	internal void TakeAttributes( List<string> attributes )
	{
		attr.AddRange( attributes );
		attributes.Clear();
	}

	internal bool HasAttribute( string name )
	{
		return attr.Contains( name );
	}
}
