using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Facepunch.InteropGen.Parsers;

internal class ClassParser : BaseParser
{
	// Precompiled regex for better performance
	private static readonly Regex _attributeRegex = new(
		@"^\[(.+)\]",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	// Cache for attribute parsing
	private static readonly ConcurrentDictionary<string, string> _attributeCache = new();

	private readonly Class Class;
	public bool IsNative { get; set; }

	public ClassParser( Definition definition, Class c )
	{
		this.definition = definition;
		Class = c;
	}

	public override void ParseLine( string line )
	{
		string trimmedLine = line.Trim();

		if ( trimmedLine == "{" )
		{
			return;
		}

		if ( trimmedLine == "}" )
		{
			Finished = true;
			return;
		}

		Function inline_func = InlineFunction.Parse( line );
		if ( inline_func != null )
		{
			inline_func.Class = Class;
			inline_func.TakeAttributes( Attributes );
			Class.Functions.Add( inline_func );

			BodyParser parser = new( definition, inline_func );
			subParser.Push( parser );

			return;
		}

		Function func = Function.Parse( line );
		if ( func != null )
		{
			func.Class = Class;
			func.TakeAttributes( Attributes );
			Class.Functions.Add( func );
			return;
		}

		Variable var = Variable.Parse( line );
		if ( var != null )
		{
			var.TakeAttributes( Attributes );
			Class.Variables.Add( var );
			return;
		}

		// Use cached attribute parsing
		if ( TryParseAttribute( trimmedLine, out string attributeValue ) )
		{
			Attributes.Add( attributeValue );
			return;
		}

		base.ParseLine( line );
	}

	private bool TryParseAttribute( string line, out string attributeValue )
	{

		// Check cache first
		if ( _attributeCache.TryGetValue( line, out attributeValue ) )
		{
			return attributeValue != null;
		}

		// Parse with regex
		Match match = _attributeRegex.Match( line );
		bool success = match.Success;
		attributeValue = success ? match.Groups[1].Value : null;

		// Cache result (including negative results)
		if ( _attributeCache.Count < 1000 )
		{
			_ = _attributeCache.TryAdd( line, attributeValue );
		}

		return success;
	}
}
