using Sandbox;
using System;

[AttributeUsage( AttributeTargets.Property )]
[CodeGenerator( CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Instance, "OnWrapSet" )]
[CodeGenerator( CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Static, "WrapSet.OnWrapSetStatic" )]
public class WrapSet : Attribute
{
	public static void OnWrapSetStatic<T>( WrappedPropertySet<T> p )
	{
		
	}
}

public partial class TestWrapSet
{
	[WrapSet]
	public static bool StaticProperty { get; set; }
	
	[WrapSet]
	public bool InstanceProperty { set; }

	internal void OnWrapSet<T>( WrappedPropertySet<T> p )
	{
		return null;
	}
}
