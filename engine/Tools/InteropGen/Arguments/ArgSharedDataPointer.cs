namespace Facepunch.InteropGen;

/// <summary>
/// Qt defines some things as regular classes that contain nothing but a smart pointer to the
/// real data. Marking a class as [SharedDataPointer] will:
/// 
/// 1. Always return as a new Class()
/// 2. Pass to native as a pointer to that class
/// 3. Pass to the native function as a instance to that class (*) ptr
/// </summary>
public class ArgSharedDataPointer : ArgDefinedClass
{

	public ArgSharedDataPointer( Class c, string name, string[] flags ) : base( c, name, flags )
	{

	}

	public override string FromInterop( bool native, string code = null )
	{
		return native ? "*" + (code ?? Name) : base.FromInterop( native, code );
	}

	public override string ToInterop( bool native, string code = null )
	{
		return !native ? $"{code ?? Name}.GetPointerAssertIfNull()" : base.ToInterop( native, code );
	}

	public override string ReturnWrapCall( string functionCall, bool native )
	{
		return native
			? $"return new {Class.NativeNameWithNamespace}( {functionCall} );"
			: $"return new {Class.ManagedNameWithNamespace}( {functionCall} );";
	}

}
