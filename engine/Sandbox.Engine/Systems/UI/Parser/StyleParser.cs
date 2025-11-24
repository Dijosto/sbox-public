namespace Sandbox.UI;

internal static partial class StyleParser
{
	/// <summary>
	/// Parse the styles as you would if they were passed in an style="width: 100px" attribute
	/// </summary>
	internal static void ParseStyles( ref Parse p, Styles style, bool parentheses = false )
	{
		if ( parentheses )
		{
			p = p.SkipWhitespaceAndNewlines();
			if ( !p.TrySkip( "{" ) )
				throw new Exception( $"Expected {{ {p.FileAndLine}" );
		}

		while ( !p.IsEnd )
		{
			p = p.SkipWhitespaceAndNewlines( ":;" );

			if ( p.Current == ':' )
				throw new System.Exception( "Parsing error - unexpected ':' at " );

			var name = p.ReadUntil( ":" );
			if ( name == null )
				break;

			p.Pointer++;

			p = p.SkipWhitespaceAndNewlines();

			var value = p.ReadUntilOrEnd( ";" );
			if ( value == null )
				break;

			p.Pointer++;

			if ( !style.Set( name, value ) )
			{
				throw new Exception( $"Unknown Property: {name} / {value} {p.FileAndLine}" );
			}

			p = p.SkipWhitespaceAndNewlines();

			if ( parentheses && p.TrySkip( "}" ) )
				break;
		}
	}
}
