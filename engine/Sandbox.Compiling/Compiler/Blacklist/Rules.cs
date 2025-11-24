using System.Text.RegularExpressions;

namespace Sandbox;

static partial class CompilerRules
{
	public static List<Regex> Blacklist = new();

	static CompilerRules()
	{
		AddRules( Methods );
		AddRules( Attributes );
	}

	static void AddRules( IEnumerable<string> rules )
	{
		foreach ( var rule in rules )
		{
			var wildcard = Regex.Escape( rule ).Replace( "\\*", ".*" );
			wildcard = $"^{wildcard}$";

			var regex = new Regex( wildcard, RegexOptions.Compiled );
			Blacklist.Add( regex );
		}
	}
}
