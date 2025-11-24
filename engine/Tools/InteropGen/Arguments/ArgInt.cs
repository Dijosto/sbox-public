namespace Facepunch.InteropGen;

[TypeName( "int" )]
public class ArgInt : Arg
{
	public override string ManagedType => "int";
	public override string ManagedDelegateType => "int";
}
