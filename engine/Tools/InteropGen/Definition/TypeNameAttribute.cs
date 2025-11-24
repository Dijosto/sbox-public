using System;

namespace Facepunch.InteropGen;

[AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
public class TypeNameAttribute : System.Attribute
{
	public string TypeName { get; private set; }

	public TypeNameAttribute( string match )
	{
		TypeName = match.ToLower();
	}
}
