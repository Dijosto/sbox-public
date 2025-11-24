namespace Facepunch.InteropGen;

[TypeName( "CUtlString" )]
public class ArgCUtlString : Arg
{
	public override string ManagedType => "string";
	public override string ManagedDelegateType => "IntPtr";
	public override string NativeType => "CUtlString";
	public override string NativeDelegateType => "const char *";

	public override string ReturnWrapCall( string call, bool native )
	{
		return native ? $"return (const char*) SafeReturnString( (const char *) {call} );" : base.ReturnWrapCall( call, native );
	}

	public override string ToInterop( bool native, string code = null )
	{
		code ??= Name;

		return !native ? $"_str_{code}.Pointer" : native ? $"{code}.String()" : base.ToInterop( native, code );
	}

	public override string FromInterop( bool native, string code = null )
	{
		code ??= Name;

		return !native ? $"{Definition.Current.StringTools}.GetString( {code} )" : native ? $"{code}" : base.ToInterop( native, code );
	}

	public override string WrapFunctionCall( string functionCall, bool native )
	{
		if ( !native && HasFlag( "out" ) )
		{
			return $"IntPtr _outptr_{Name} = default;\n\n" +
				$"try\n" +
				$"{{\n" +
				$"	{functionCall}\n" +
				$"}}\n" +
				$"finally\n" +
				$"{{\n" +
				$"	{Name} = {Definition.Current.StringTools}.GetString( _outptr_{Name} );\n" +
				$"}}\n";
		}
		else if ( !native )
		{
			return $"var _str_{Name} = new {Definition.Current.StringTools}.InteropString( {Name} ); try {{ {functionCall} }} finally {{ _str_{Name}.Free(); }} ";
		}

		return base.WrapFunctionCall( functionCall, native );
	}
}
