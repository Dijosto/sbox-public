namespace Facepunch.InteropGen;


public class ArgArray : ArgWrapper
{
	public ArgArray( Arg val )
	{
		Base = val;
		Name = Base.Name;
	}

	public override string NativeType => $"{Base.NativeType}*";
	public override string ManagedType => $"{Base.ManagedType}*";
	public override string ManagedDelegateType => $"{Base.ManagedType}*";
	public override string NativeDelegateType => NativeType;
	public override string GetManagedDelegateType( bool incoming )
	{
		return ManagedDelegateType;
	}

	public override string GetNativeDelegateType( bool incoming )
	{
		return NativeDelegateType;
	}

	public override string ToInterop( bool native, string code = null )
	{
		return base.ToInterop( native, code );
	}

	public override string WrapFunctionCall( string functionCall, bool native )
	{
		return functionCall;
	}

}
