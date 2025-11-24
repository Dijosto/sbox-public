namespace Facepunch.InteropGen;

public class ArgWrapper : Arg
{
	public Arg Base;

	public override string NativeType => Base.NativeType;
	public override string NativeDelegateType => Base.NativeDelegateType;

	public override string ManagedType => Base.ManagedType;
	public override string ManagedDelegateType => Base.ManagedDelegateType;

	public override bool IsVoid => Base.IsVoid;

	public override bool IsRealArgument => Base.IsRealArgument;

	public override string GetManagedDelegateType( bool incoming )
	{
		return Base.GetManagedDelegateType( incoming );
	}

	public override string GetNativeDelegateType( bool incoming )
	{
		return Base.GetNativeDelegateType( incoming );
	}
}
