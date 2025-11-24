using System.Linq;

namespace Facepunch.InteropGen;

public class ArgFlagsWrapper : ArgWrapper
{
	public ArgFlagsWrapper( Arg val )
	{
		Base = val;
		Name = val.Name;
		Flags = val.Flags;
	}

	public override string NativeType
	{
		get
		{
			string t = Base.NativeType;

			if ( HasFlag( "out" ) || HasFlag( "ref" ) || HasFlag( "asref" ) )
			{
				t += "*";
			}

			//  if ( HasFlag( "ref" ) )
			//   t += "***";

			//  $"{base.NativeType}*";

			return t;
		}

	}

	public override string NativeDelegateType
	{
		get
		{
			string t = Base.NativeDelegateType;

			if ( HasFlag( "out" ) || HasFlag( "ref" ) || HasFlag( "asref" ) )
			{
				t += "*";
			}

			return t;
		}
	}

	public override string GetNativeDelegateType( bool incoming )
	{
		string t = Base.GetNativeDelegateType( incoming );

		if ( HasFlag( "out" ) || HasFlag( "ref" ) || HasFlag( "asref" ) )
		{
			t += "*";
		}

		return t;
	}

	public override string ManagedType
	{
		get
		{
			string t = Base.ManagedType;

			if ( HasFlag( "out" ) )
			{
				t = "out " + t;
			}

			if ( HasFlag( "ref" ) )
			{
				t = "ref " + t;
			}

			//$"{base.NativeType}*"; ref

			return t;
		}

	}
	public override string ManagedDelegateType
	{
		get
		{
			string t = Base.ManagedDelegateType;

			if ( HasFlag( "out" ) )
			{
				t = "out " + t;
			}

			if ( HasFlag( "ref" ) )
			{
				t = "ref " + t;
			}

			if ( HasFlag( "asref" ) )
			{
				t = "IntPtr";
			}

			return t;
		}

	}

	public override string GetManagedDelegateType( bool incoming )
	{
		string t = Base.GetManagedDelegateType( incoming );

		if ( HasFlag( "out" ) )
		{
			t = "out " + t;
		}

		if ( HasFlag( "ref" ) )
		{
			t = "ref " + t;
		}

		if ( HasFlag( "mptr" ) || HasFlag( "asref" ) )
		{
			t = "IntPtr";
		}

		return t;
	}

	public override string FromInterop( bool native, string code = null )
	{
		if ( Flags == null )
		{
			return Base.FromInterop( native, code );
		}

		string name = code ?? Name;

		if ( !native )
		{
			if ( HasFlag( "asref" ) )
			{
				return $"ref Unsafe.AsRef<{Base.ManagedType}>( (void*) {name} )";
			}

		}

		if ( native )
		{
			if ( HasFlag( "cref" ) )
			{
				return $"*{name}";
			}

			string cast = Flags.FirstOrDefault( x => x.StartsWith( "CastTo[" ) && x.EndsWith( "]" ) );
			if ( cast != null )
			{
				cast = cast.Substring( 7, cast.Length - 7 - 1 );
				return $"/*CastTo*/ ({cast}) {name}";
			}
		}

		return HasFlag( "ref" ) ? $"{name}" : Base.FromInterop( native, code );
	}


	public override string ToInterop( bool native, string code = null )
	{
		if ( Flags == null )
		{
			return Base.ToInterop( native, code );
		}

		string name = code ?? Name;

		if ( !native && HasFlag( "out" ) && Base is ArgString )
		{
			return $"out _outptr_{name}";
		}


		if ( !native && HasFlag( "out" ) )
		{
			return $"out {name}";
		}

		if ( !native && HasFlag( "ref" ) )
		{
			return $"ref {name}";
		}

		if ( native && HasFlag( "cref" ) )
		{
			return $"&{name}";
		}

		//
		// Returning a class, we want to cast it from one thing to this type
		//
		if ( native && HasFlag( "cast" ) )
		{
			return $"({NativeType}) {name}";
		}


		// code ??= Name;


		return native && HasFlag( "boxed" ) ? $"{code}.GetRaw()" : Base.ToInterop( native, code );
	}

	public override string ReturnWrapCall( string functionCall, bool native )
	{
		if ( native )
		{
			string cast = Flags?.FirstOrDefault( x => x.StartsWith( "CastTo[" ) && x.EndsWith( "]" ) ) ?? null;
			if ( cast != null )
			{
				cast = cast.Substring( 7, cast.Length - 7 - 1 );
				functionCall = $"/*CastTo*/ {cast} {functionCall}";
			}
		}

		return Base.ReturnWrapCall( functionCall, native );
	}

	public override string WrapFunctionCall( string functionCall, bool native )
	{
		return Base.WrapFunctionCall( functionCall, native );
	}

	public override string DefaultValue => Base.DefaultValue;
}
