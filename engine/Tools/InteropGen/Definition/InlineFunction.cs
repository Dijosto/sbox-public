using System.Text.RegularExpressions;

namespace Facepunch.InteropGen;

public class InlineFunction
{
	internal static Function Parse( string line )
	{
		Match m = Regex.Match( line.Trim(), @"^inline\s(static)?[\s+]?(.+?)\s+([a-zA-Z0-9_]+?)\(((.+?))?\)( const)?(.+)?", RegexOptions.IgnoreCase );
		if ( !m.Success )
		{
			return null;
		}

		Function f = new()
		{
			Native = true,
			Static = m.Groups[1].Success,
			Name = m.Groups[3].Value.Trim(),
			Return = Arg.Parse( m.Groups[2].Value + " returnvalue" ),
			Parameters = Arg.ParseMany( m.Groups[4].Value )
		};
		f.AddSpecial( m.Groups[7].Value );

		return f;
	}
}
