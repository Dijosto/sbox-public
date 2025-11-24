namespace Facepunch.InteropGen;

[TypeName( "float" )]
public class ArgFloat : Arg
{
	public override string ManagedType => "float";
	public override string ManagedDelegateType => ManagedType;
}

[TypeName( "double" )]
public class ArgDouble : Arg
{
	public override string ManagedType => "double";
	public override string ManagedDelegateType => ManagedType;
}
