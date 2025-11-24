namespace Facepunch.InteropGen;

public class ArgDefinedStruct : Arg
{
	public Struct Type { get; set; }


	public ArgDefinedStruct( Struct c, string name, string[] flags )
	{
		Type = c;
		Name = name;
		Flags = flags;
	}

	public override string ManagedType => Type.ManagedNameWithNamespace;
	public override string NativeType => Type.NativeNameWithNamespace;
	public override string ManagedDelegateType => ManagedType;

	public override string NativeDelegateType => NativeType;

	public override string GetManagedDelegateType( bool incoming )
	{
		return Type.IsPointer
			? $"IntPtr /* PtrHandle:{Type.NativeName}  */"
			: !incoming && Flags == null && !Type.IsEnum && !Type.HasAttribute( "small" )
			? $"{ManagedType}*"
			: base.GetManagedDelegateType( incoming );
	}

	public override string ReturnWrapCall( string functionCall, bool native )
	{
		if ( native )
		{
			if ( Type.CreateUsing != null )
			{
				functionCall = $"{Type.CreateUsing}( {functionCall} )";
			}
		}

		return base.ReturnWrapCall( functionCall, native );
	}

	public override string ToInterop( bool native, string code = null )
	{
		return Type.IsPointer
			? code ?? Name
			: !native && code == null && Name != null && Flags == null && !Type.IsEnum && !Type.HasAttribute( "small" )
			? $"&{Name}"
			: base.ToInterop( native, code );
	}

	public override string DefaultValue => $"{NativeType}()";
}
