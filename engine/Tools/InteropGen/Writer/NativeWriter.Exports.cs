using System.Collections.Generic;
using System.Linq;

namespace Facepunch.InteropGen;

internal partial class NativeWriter
{
	private void Exports()
	{
		WriteLine( "//" );
		WriteLine( "// EXPORTS" );
		WriteLine( "// " );
		WriteLine( "// Functions that we're exposing to managed" );
		WriteLine( "//" );
		StartBlock( "namespace Exports" );
		{
			foreach ( Class c in definitions.Classes.Where( x => x.Native == true ) )
			{
				if ( ShouldSkip( c ) )
				{
					continue;
				}

				if ( c.BaseClass != null )
				{
					Class bc = c.BaseClass;

					while ( bc != null )
					{
						Class subclass = bc;
						WriteCast( subclass, c );
						bc = bc.BaseClass;
					}

					WriteLine();
				}

				foreach ( Function f in c.Functions )
				{
					// We prepend __ to argument names for inline functions.
					// That way the body can use the right named vars after converting to the proper type.
					string varNamePrepend = "";
					if ( f.Body != null )
					{
						varNamePrepend = "__";
					}

					IEnumerable<string> nativeArgs = c.SelfArg( true, f.Static ).Concat( f.Parameters ).Where( x => x.IsRealArgument ).Select( x => $"{x.GetNativeDelegateType( true )} {varNamePrepend}{x.Name}" );
					string nativeArgS = string.Join( ", ", nativeArgs );

					StartBlock( $"{f.Return.GetNativeDelegateType( false )} {f.MangledName}( {nativeArgS} )" );
					{
						WriteLine( $"CALL_FROM_MANAGED_START();" );

						// Generate stub implementation for platform-specific functions
						if ( ShouldStubFunction( c, f ) )
						{
							WriteLine( $"// Stubbed implementation for {c.ManagedName}::{f.Name}" );
							if ( !f.Return.IsVoid )
							{
								WriteLine( $"return {f.Return.DefaultValue};" );
							}
						}
						else if ( !HandleSpecial( c, f ) )
						{
							if ( c.Accessor )
							{
								WriteLine( $"// Make sure this isn't null" );
								WriteLine( $"Assert( {c.NativeNameWithNamespace} );" );
								WriteLine();
							}

							string pre = "";
							string post = "";
							IEnumerable<string> args = f.Parameters.Select( x => x.FromInterop( true ) ).Where( x => x != null );
							string argsS = string.Join( ", ", args );

							pre = $"(({c.NativeNameWithNamespace}*)self)->";

							argsS = argsS.Replace( "__selftype__", $"{c.NativeNameWithNamespace}*" );

							if ( c.Static || f.Static )
							{
								pre = $"{c.NativeNameWithNamespace}::";
							}

							if ( c.Static && c.NativeName.StartsWith( "global" ) )
							{
								pre = "::";
							}

							if ( c.Accessor )
							{
								pre = $"{c.NativeNameWithNamespace}->";
							}

							if ( c.IsResourceHandle )
							{
								string strongHandle = $"{c.ResourceHandleName}Strong";
								WriteLine( $"{strongHandle}* __handle = ({strongHandle}*)self;" );
								WriteLine( $"if ( __handle == nullptr || !__handle->HasData() ) return {f.Return.DefaultValue};" );
								WriteLine( $"{c.NativeNameWithNamespace}* __self = const_cast<{c.NativeNameWithNamespace}*>( __handle->GetData() );" );
								WriteLine( $"if ( __self == nullptr ) return {f.Return.DefaultValue};" );

								pre = $"__self->";
							}

							string functionCall = $"{pre}{f.Name}( {argsS} ){post}";

							if ( !f.Return.IsVoid )
							{
								functionCall = f.Return.ToInterop( true, functionCall );
								functionCall = f.Return.ReturnWrapCall( functionCall, true );
								WriteLine( $"{functionCall}" );
							}
							else
							{
								WriteLine( $"{functionCall};" );
							}
						}

						WriteLine( $"CALL_FROM_MANAGED_END();" );
					}
					EndBlock();
				}

				foreach ( Variable f in c.Variables )
				{
					IEnumerable<string> nativeArgs = c.SelfArg( true, f.Static ).Select( x => $"{x.NativeType} {x.Name}" );
					string nativeArgS = string.Join( ", ", nativeArgs );

					StartBlock( $"{f.Return.GetNativeDelegateType( false )} _Get__{f.MangledName}( {nativeArgS} )" );
					{
						string pre = "";
						string post = "";

						pre = $"(({c.NativeNameWithNamespace}*)self)->";

						if ( c.Static || f.Static )
						{
							pre = $"{c.NativeNameWithNamespace}::";
						}

						if ( c.Static && c.NativeName.StartsWith( "global" ) )
						{
							pre = "::";
						}

						if ( c.Accessor )
						{
							pre = $"{c.NativeNameWithNamespace}->";
						}

						string functionCall = $"{pre}{f.Name}{post}";

						functionCall = f.Return.ToInterop( true, functionCall );
						functionCall = f.Return.ReturnWrapCall( functionCall, true );
						WriteLine( $"{functionCall}" );
					}
					EndBlock();

					if ( !string.IsNullOrEmpty( nativeArgS ) )
					{
						nativeArgS += ", ";
					}

					StartBlock( $"void _Set__{f.MangledName}( {nativeArgS}{f.Return.GetNativeDelegateType( true )} value )" );
					{
						string pre = "";
						string post = "";

						pre = $"(({c.NativeNameWithNamespace}*)self)->";

						if ( c.Static || f.Static )
						{
							pre = $"{c.NativeNameWithNamespace}::";
						}

						if ( c.Static && c.NativeName.StartsWith( "global" ) )
						{
							pre = "::";
						}

						if ( c.Accessor )
						{
							pre = $"{c.NativeNameWithNamespace}->";
						}

						string functionCall = $"{pre}{f.Name}{post} = {f.Return.FromInterop( true, "value" )}";

						WriteLine( $"{functionCall};" );
					}
					EndBlock();
				}
			}
		}
		EndBlock();

		WriteLine();
	}

	private void WriteCast( Class subclass, Class c )
	{
		Write( $"void* From_{subclass.ManagedName}_To_{c.ManagedName}( {subclass.NativeNameWithNamespace}* ptr ) {{ ", true );
		{
			Write( $"return dynamic_cast<{c.NativeNameWithNamespace}*>( ptr );" );
		}
		Write( " }\n" );
		Write( $"void* To_{subclass.ManagedName}_From_{c.ManagedName}( {c.NativeNameWithNamespace}* ptr ) {{ ", true );
		{
			Write( $"return dynamic_cast<{subclass.NativeNameWithNamespace}*>( ptr );" );
		}
		Write( " }\n" );
	}

	private bool HandleSpecial( Class c, Function f )
	{
		if ( f.NativeCallReplacement != null )
		{
			WriteLines( f.NativeCallReplacement );
			return true;
		}

		//
		// If we have a body, we're an inline function, so use that instead of generating the body
		//
		if ( f.Body != null )
		{
			//
			// For functions with a body the args are passed in with __ prepended to the front
			// this lets us convert them to the proper types for use in the body
			//
			IEnumerable<Arg> args = c.SelfArg( true, f.Static ).Concat( f.Parameters );

			if ( c.IsResourceHandle && !f.Static )
			{
				string strongHandle = $"{c.ResourceHandleName}Strong";
				StartBlock( "// Handle Conversion - make __self point to the actual class instead of the handle" );
				WriteLine( $"{strongHandle}* __handle = ({strongHandle}*)__self;" );
				WriteLine( $"if ( __handle == nullptr || !__handle->HasData() ) return {f.Return.DefaultValue};" );
				WriteLine( $"__self = const_cast<{c.NativeNameWithNamespace}*>( __handle->GetData() );" );
				WriteLine( $"if ( __self == nullptr ) return {f.Return.DefaultValue};" );
				EndBlock();
				WriteLine();
			}

			WriteLine( $"// Convert parameters" );



			foreach ( Arg arg in args )
			{
				if ( arg.IsSelf )
				{
					WriteLine( $"{c.NativeNameWithNamespace}* {arg.Name} = ({c.NativeNameWithNamespace}*) __{arg.Name};" );
					continue;
				}

				WriteLine( $"auto {arg.Name} = {arg.FromInterop( true, $"__{arg.Name}" )};" );
			}

			string body = f.Body.ToString();
			body = body.Replace( "this->", "self->" );

			//
			// If the inline shit is returning shit, we want to try to wrap it with ToInterop and ReturnWrapCall
			// this code below is janky and probably isn't the real solution, but it worked for what I was doing
			//
			if ( !f.Return.IsVoid )
			{
				string[] lines = body.Split( "\n" );

				for ( int i = 0; i < lines.Length; i++ )
				{
					if ( !lines[i].Trim().StartsWith( "return " ) )
					{
						continue;
					}

					lines[i] = lines[i].Trim().Replace( "return ", "" ).TrimEnd( ';', '\r', '\n' );

					lines[i] = f.Return.ToInterop( true, lines[i] );
					lines[i] = f.Return.ReturnWrapCall( lines[i], true );
				}

				body = string.Join( "\n", lines );
			}

			//
			// Print the body with this-> changed to self->
			//
			WriteLine( $"" );
			WriteLine( $"// Body" );
			WriteLines( body );
			return true;
		}

		if ( f.Special.Contains( "delete" ) )
		{
			WriteLine( $"delete (({c.NativeNameWithNamespace}*)self);" );
			return true;
		}

		if ( f.Special.Contains( "new" ) )
		{
			IEnumerable<string> args = f.Parameters.Select( x => x.FromInterop( true ) );
			string argsS = string.Join( ", ", args );

			WriteLine( $"return new {c.NativeNameWithNamespace}( {argsS} );" );
			return true;
		}

		return false;
	}
}
