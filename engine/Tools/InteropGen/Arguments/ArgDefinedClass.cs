namespace Facepunch.InteropGen;

public class ArgDefinedClass : Arg
{
	public Class Class { get; set; }


	public ArgDefinedClass( Class c, string name, string[] flags )
	{
		Class = c;
		Name = name;
		Flags = flags;
	}

	public override string ManagedType => Class.IsHandleType ? $"global::{Class.HandleIndex}" : "global::" + Class.ManagedNameWithNamespace;


	public override string ManagedDelegateType => "IntPtr";

	public override string GetManagedDelegateType( bool incoming )
	{
		return (Class.IsHandleType || Class.IsChildHandleType) && incoming ? "int" : "IntPtr";
	}


	public override string NativeType => Class.IsResourceHandle ? $"{Class.ResourceHandleName}Strong*" : $"{Class.NativeNameWithNamespace}*";

	public override string NativeDelegateType => Class.IsResourceHandle
				? $"{Class.ResourceHandleName}Strong*"
				: Class.IsHandleType || Class.IsChildHandleType ? "int" : NativeType;

	public override string GetNativeDelegateType( bool incoming )
	{
		return Class.IsResourceHandle
			? $"{Class.ResourceHandleName}Strong*"
			: !incoming && (Class.IsHandleType || Class.IsChildHandleType)
			? "int"
			: IsReturn ? $"const {Class.NativeNameWithNamespace}*" : NativeType;
	}

	public override string FromInterop( bool native, string code = null )
	{
		code ??= Name;

		if ( Class.IsHandleType )
		{
			if ( !native )
			{
				return $"Sandbox.HandleIndex.Get<{Class.HandleIndex}>( {code} )";
			}
		}

		if ( Class.IsResourceHandle )
		{
			if ( native )
			{
				// Using custom functions to call ->GetHandle() so we can 
				// handle if {code} is null in it by returning an invalid handle
				return $"ResourceHandle_GetHandle( {code} )";
			}
		}

		return native ? $"({NativeType}){code}" : base.FromInterop( native, code );
	}

	public override string ToInterop( bool native, string code = null )
	{
		code ??= Name;

		if ( Class.IsHandleType || Class.IsChildHandleType )
		{
			return native ? $"GetManagedHandle( {code} )" : $"{code} == null ? IntPtr.Zero : {code}.native";
		}

		if ( Class.IsResourceHandle )
		{
			if ( native && IsReturn )
			{
				return $"new {Class.ResourceHandleName}StrongCopyable( {code} )";
			}
		}

		//
		// Passing a managed class to native - we just use the .NativePointer property
		// Which should be using the NativePointer class to create a GCHandle.
		//
		return !native && !Class.Native
			? $" ( {code} == null ? IntPtr.Zero : Sandbox.InteropSystem.GetAddress( {code}, true ) )"
			: native && !Class.Native
			? $" ( {code} == nullptr ? nullptr : {code}->ptr() )"
			: native ? $"{code}" : base.ToInterop( native, code );
	}

}
