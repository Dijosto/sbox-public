using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Facepunch.InteropGen.Parsers;

internal class BaseParser
{
	// Precompiled regex for better performance
	private static readonly Regex _methodCallRegex = new(
		@"([a-z]+?)\s+(.+)",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	protected Definition definition;
	protected Stack<BaseParser> subParser = new();
	protected Stack<string> fileStack = new();
	protected bool Finished;
	protected List<string> Attributes = [];

	public void Parse( Definition definition, string text, string filename )
	{
		this.definition = definition;
		fileStack.Push( filename );

		ParseText( text );
	}

	private void ParseText( string text )
	{
		// Use efficient line enumeration with StringSplitOptions
		string[] lines = text.Split( ['\n', '\r'], StringSplitOptions.RemoveEmptyEntries );

		foreach ( string line in lines )
		{
			// Skip empty lines and whitespace-only lines
			if ( string.IsNullOrWhiteSpace( line ) )
			{
				continue;
			}

			// Check for comments using efficient string operations
			ReadOnlySpan<char> trimmedLine = line.AsSpan().TrimStart();
			if ( trimmedLine.Length >= 2 && trimmedLine[0] == '/' && trimmedLine[1] == '/' )
			{
				continue;
			}

			definition.FullText += $"{line}\n";

			// Clean up sub-parser stack efficiently
			CleanupFinishedSubParsers();

			if ( subParser.Count > 0 )
			{
				subParser.Peek().SubParseLine( line );
				continue;
			}

			ParseLine( line );
		}
	}

	// Optimize sub-parser cleanup
	private void CleanupFinishedSubParsers()
	{
		while ( subParser.Count > 0 && subParser.Peek().Finished )
		{
			_ = subParser.Pop();
		}
	}

	public void SubParseLine( string line )
	{
		CleanupFinishedSubParsers();

		if ( subParser.Count > 0 )
		{
			subParser.Peek().SubParseLine( line );
			return;
		}

		ParseLine( line );
	}

	public virtual void ParseLine( string line )
	{
		// Use cached regex for method parsing
		Match match = _methodCallRegex.Match( line );
		if ( match.Success )
		{
			MethodInfo method = GetMethod( match.Groups[1].Value );
			if ( method != null )
			{
				string arg = match.Groups[2].Value.Trim();

				// More efficient quote removal using Span operations
				if ( arg.Length >= 2 && arg[0] == '"' && arg[^1] == '"' )
				{
					arg = arg[1..^1];
				}

				_ = method.Invoke( this, new object[] { arg } );
				return;
			}
		}

		Log.Warning( $"Unhandled Line \"{line}\" in \"{fileStack.Peek()}\"" );
	}

	private readonly Dictionary<string, MethodInfo> MethodCache = [];

	public MethodInfo GetMethod( string name )
	{
		if ( MethodCache.TryGetValue( name, out MethodInfo method ) )
		{
			return method;
		}

		method = GetType().GetMethod( name );
		MethodCache[name] = method;
		return method;
	}

	public void IncludeFile( string filename )
	{
		string path = System.IO.Path.GetDirectoryName( fileStack.Peek() );
		string fullname = System.IO.Path.Combine( path, filename );

		//using ( Log.Group( ConsoleColor.DarkGreen, $"{filename}" ) )
		{
			fileStack.Push( fullname );

			string text = System.IO.File.ReadAllText( fullname );
			ParseText( text );

			_ = fileStack.Pop();
		}
	}

	public void IncludeFolder( string filename )
	{
		SearchOption option = filename.EndsWith( "/*" ) ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;

		string path = System.IO.Path.GetDirectoryName( fileStack.Peek() );
		string fullname = System.IO.Path.Combine( path, filename );

		foreach ( string file in System.IO.Directory.GetFiles( fullname.TrimEnd( '*' ), "*.def", option ).OrderBy( x => x ) )
		{
			IncludeFile( System.IO.Path.Combine( fullname, file ) );
		}
	}
}
