using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Facepunch.InteropGen;

public class Variable
{
	public bool Native { get; set; }
	public bool Static { get; private set; }
	public string Name { get; set; }
	public Arg Return { get; set; }
	public Class Class { get; set; }
	public string MangledName { get; set; }

	internal static Variable Parse( string line )
	{
		Match m = Regex.Match( line, @"^[\s+]?(static)?[\s+]?(.+?)\s+([a-zA-Z0-9_]+?);", RegexOptions.IgnoreCase );
		if ( !m.Success )
		{
			return null;
		}

		Variable f = new()
		{
			Native = true,
			Static = m.Groups[1].Success,
			Name = m.Groups[3].Value.Trim(),
			Return = Arg.Parse( m.Groups[2].Value + " returnvalue" )
		};

		return f;
	}

	internal string GetManagedName()
	{
		return Name == "GetType" ? "GetType_Native" : Name == "params" ? $"@{Name}" : Name;
	}

	private readonly List<string> attr = [];

	internal void TakeAttributes( List<string> attributes )
	{
		attr.AddRange( attributes );
		attributes.Clear();
	}
}
