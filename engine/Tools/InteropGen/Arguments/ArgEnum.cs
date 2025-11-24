namespace Facepunch.InteropGen;

public class ArgEnum : Arg
{
	public Struct Type { get; set; }

	public ArgEnum( Struct t, string name )
	{
		Type = t;
		Name = name;
	}

	public override string ManagedType => Type.ManagedNameWithNamespace;
	public override string ManagedDelegateType => "long";
	public override string NativeType => Type.NativeNameWithNamespace;
	public override string NativeDelegateType => "int64";

	public override string ToInterop( bool native, string code = null )
	{
		code ??= Name;

		return !native ? $"(long)({code})" : $"(int64)({code})";
	}

	public override string FromInterop( bool native, string code = null )
	{
		code ??= Name;

		return !native ? $"({ManagedType})({code})" : $"({NativeType})({code})";
	}
}
