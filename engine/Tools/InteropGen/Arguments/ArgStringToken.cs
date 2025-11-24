namespace Facepunch.InteropGen;

//
// Note: Only supporting passing string to native right now
//
[TypeName( "stringtoken" )]
public class ArgStringToken : Arg
{
	public override string ManagedType => "Sandbox.StringToken";
	public override string ManagedDelegateType => "Sandbox.StringToken";
	public override string NativeType => "uint32";

	public override string ToInterop( bool native, string code = null )
	{
		//	if ( code == null ) code = Name;

		//	if ( !native )
		//	{
		//		return $"Sandbox.StringToken.FindOrCreate( {code} )";
		//	}

		return base.ToInterop( native, code );
	}

	public override string FromInterop( bool native, string code = null )
	{
		code ??= Name;

		return native ? $"StringTokenFromHashCode( {code} )" : base.ToInterop( native, code );
	}
}
