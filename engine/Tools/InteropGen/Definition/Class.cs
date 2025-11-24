using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Facepunch.InteropGen;

public class Class
{
	// Precompiled regex patterns for better performance
	private static readonly Regex _classParseRegex = new(
		@"([\w<>\*.:\(\)]+)( [\s+]?as [\s+]?([\w.:]+))?(.+)?",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	private static readonly Regex _extraParseRegex = new(
		@"[\s+]?: [\s+]?([\w.:]+)",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	// Cache for class parsing results
	private static readonly ConcurrentDictionary<string, ParsedClassDefinition> _classParseCache = new();

	// Struct to hold parsed components efficiently
	private readonly struct ParsedClassDefinition
	{
		public readonly string ClassName;
		public readonly string AliasName;
		public readonly string ExtraInfo;

		public ParsedClassDefinition( string className, string aliasName, string extraInfo )
		{
			ClassName = className;
			AliasName = aliasName;
			ExtraInfo = extraInfo;
		}
	}

	public string NativeName { get; set; }
	public string ManagedName { get; set; }

	public string NativeNamespace { get; set; }
	public string ManagedNamespace { get; set; }
	public bool Native { get; private set; }
	public bool Static { get; private set; }
	public bool Accessor { get; private set; }

	public Class BaseClass { get; internal set; }
	public string BaseClassName { get; private set; }

	public int ClassDepth { get; set; }

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

	public int ClassIdent { get; set; }

	public List<Function> Functions = [];
	public List<Variable> Variables = [];

	internal static Class Parse( bool isNative, bool isStatic, string type, string line )
	{
		// Check cache first
		if ( _classParseCache.TryGetValue( line, out ParsedClassDefinition cached ) )
		{
			return CreateFromCached( isNative, isStatic, type, cached );
		}

		// Parse with regex only if not cached
		Match match = _classParseRegex.Match( line );
		if ( !match.Success )
		{
			Log.Warning( $"Couldn't parse class definition: {line}" );
			return null;
		}

		ParsedClassDefinition parsed = new(
			className: match.Groups[1].Value.Trim(),
			aliasName: match.Groups[3].Value.Trim(),
			extraInfo: match.Groups[4].Value
		);

		// Cache the result if cache isn't too large
		if ( _classParseCache.Count < 1000 )
		{
			_ = _classParseCache.TryAdd( line, parsed );
		}

		return CreateFromCached( isNative, isStatic, type, parsed );
	}

	private static Class CreateFromCached( bool isNative, bool isStatic, string type, ParsedClassDefinition parsed )
	{
		string className = parsed.ClassName;
		string aliasName = parsed.AliasName;

		if ( aliasName == className )
		{
			Log.Warning( $"Redundant 'as' on class {className}" );
		}

		if ( string.IsNullOrWhiteSpace( aliasName ) )
		{
			aliasName = className;
		}

		if ( !isNative )
		{
			(aliasName, className) = (className, aliasName);
		}

		Class f = new()
		{
			Native = isNative,
			Accessor = type == "accessor"
		};
		f.Static = isStatic || f.Accessor;
		f.NativeName = GetClassName( className );
		f.ManagedName = GetClassName( aliasName );
		f.NativeNamespace = GetNamespace( className ).Replace( ".", "::" );
		f.ManagedNamespace = GetNamespace( aliasName );
		f.ParseExtra( parsed.ExtraInfo );

		return f;
	}

	public List<string> Attributes = [];

	internal void TakeAttributes( List<string> attributes )
	{
		Attributes.AddRange( attributes );
		attributes.Clear();

		if ( IsHandleType )
		{
			Class c = new()
			{
				Native = false,
				Accessor = false,
				Static = true,
				NativeName = GetClassName( HandleIndex ),
				ManagedName = GetClassName( HandleIndex ),
				NativeNamespace = GetNamespace( HandleIndex ).Replace( ".", "::" ) + $"::{Definition.Current.Ident}",
				ManagedNamespace = GetNamespace( HandleIndex )
			};

			Definition.Current.Classes.Add( c );
		}

		if ( IsResourceHandle )
		{
			//
			// Add a function to destroy the handle
			//
			{
				Function func = new()
				{
					Name = "DestroyStrongHandle",
					Class = this,
					NativeCallReplacement = $"delete (({ResourceHandleName}Strong*)self);"
				};
				Functions.Add( func );
			}

			//
			// Add a function to check the handle has data
			//
			{
				Function func = new()
				{
					Name = "IsStrongHandleValid",
					Return = new ArgBool(),
					Class = this,
					NativeCallReplacement = $"return (({ResourceHandleName}Strong*)self)->HasData();"
				};
				func.attr.Add( "nogc" );

				Functions.Add( func );
			}

			//
			// Add a function to check the handle has data
			//
			{
				Function func = new()
				{
					Name = "IsError",
					Return = new ArgBool(),
					Class = this,
					NativeCallReplacement = $"return (({ResourceHandleName}Strong*)self)->IsError();"
				};
				func.attr.Add( "nogc" );

				Functions.Add( func );
			}

			//
			// Add a function to check the handle has loaded
			//
			{
				Function func = new()
				{
					Name = "IsStrongHandleLoaded",
					Return = new ArgBool(),
					Class = this,
					NativeCallReplacement = $"return (({ResourceHandleName}Strong*)self)->IsLoaded();"
				};
				func.attr.Add( "nogc" );

				Functions.Add( func );
			}

			//
			// Add a function to create a copy of the handle
			//
			{
				Function func = new()
				{
					Name = "CopyStrongHandle",
					Class = this,
					Return = new ArgDefinedClass( this, "return", null ),
					NativeCallReplacement = $"return new {ResourceHandleName}Strong( ( ({ResourceHandleName}Strong*) self)->GetHandle() );"
				};
				func.attr.Add( "nogc" );

				Functions.Add( func );
			}

			//
			// Add a function to create a copy of the handle
			//
			{
				Function func = new()
				{
					Name = "GetBindingPtr",
					Class = this,
					Return = new ArgPointer(),
					NativeCallReplacement = $"return (({ResourceHandleName}Strong*) self)->GetBinding();"
				};
				func.attr.Add( "nogc" );
				Functions.Add( func );
			}
		}
	}

	internal bool HasAttribute( string name )
	{
		return Attributes.Contains( name );
	}

	internal int NativeOrder( List<Class> classes )
	{
		// hack hack hack
		// put static classes last because they might rely on real classes
		// but the real classes won't rely on the static classes.
		return Static ? 1000 : BaseClasses.Count();
	}

	/// <summary>
	/// Parses the extra information at the end of a class name. Usually " : BaseClass" if anything.
	/// </summary>
	private void ParseExtra( string value )
	{
		if ( string.IsNullOrWhiteSpace( value ) )
		{
			return;
		}

		Match baseclass = _extraParseRegex.Match( value );
		if ( baseclass.Success )
		{
			BaseClassName = baseclass.Groups[1].Value.Trim();
		}
	}

	private static string GetNamespace( string name )
	{
		int index = name.LastIndexOfAny( new[] { '.', ':' } );
		return index <= 0 ? "" : name[..index].Trim( '.', ':' );
	}

	private static string GetClassName( string name )
	{
		int index = name.LastIndexOfAny( new[] { '.', ':' } );
		return index <= 0 ? name : name[index..].Trim( '.', ':' );
	}

	public IEnumerable<Arg> SelfArg( bool native, bool memberIsStatic )
	{
		if ( Static || Accessor || memberIsStatic )
		{
			yield break;
		}

		yield return Native
			? new ArgPointer { Name = "self", IsSelf = true }
			: new ArgUInt { Name = native ? "m_ObjectId" : "self", IsSelf = true };
	}

	public List<Class> Children { get; set; }

	public bool DerivesFrom( Class c )
	{
		return c == this || (BaseClass != null && BaseClass.DerivesFrom( c ));
	}

	public IEnumerable<Class> BaseClasses
	{
		get
		{
			if ( BaseClass == null )
			{
				yield break;
			}

			Class c = BaseClass;

			while ( c != null )
			{
				yield return c;
				c = c.BaseClass;
			}
		}
	}

	public bool IsHandleType => Attributes.Any( x => x.StartsWith( "Handle:" ) );
	public bool IsChildHandleType => BaseClass != null && BaseClass.IsHandleType;
	public string HandleIndex => Attributes.First( x => x.StartsWith( "Handle:" ) )["Handle:".Length..];

	public bool IsResourceHandle => Attributes.Any( x => x.StartsWith( "ResourceHandle:" ) );
	public string ResourceHandleName => Attributes.First( x => x.StartsWith( "ResourceHandle:" ) )["ResourceHandle:".Length..];
}
