namespace Facepunch.InteropGen;

[TypeName( "intptr" )]
[TypeName( "void*" )]
public class ArgPointer : Arg
{
	public override string ManagedType => "IntPtr";
	public override string NativeType => "void*";

	public override string NativeDelegateType => "void*";

	public override string GetNativeDelegateType( bool incoming )
	{
		return !incoming && !HasFlag( "asref" ) ? "const void*" : NativeDelegateType;
	}

}
