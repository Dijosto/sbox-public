namespace Facepunch.InteropGen;

public class ArgFunctionPointer : Arg
{
	private readonly string NativeFunctionPointerType;

	public ArgFunctionPointer( string type, string name, string[] flags )
	{
		NativeFunctionPointerType = type;
		Name = name;
		Flags = flags;
	}

	public override string ManagedType => "IntPtr";
	public override string NativeType => NativeFunctionPointerType;

}
