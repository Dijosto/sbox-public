using System.Collections.Generic;
using System.Linq;

namespace Facepunch.InteropGen;

internal partial class NativeWriter : BaseWriter
{
	public NativeWriter( Definition definitions, string targetName ) : base( definitions, targetName )
	{
	}

	public override void Generate()
	{
		string headerName = System.IO.Path.GetFileName( definitions.SaveFileCppH );
		int functionCount = definitions.Classes
										.SelectMany( x => x.Functions )
										.Count();

		if ( !string.IsNullOrWhiteSpace( definitions.PrecompiledHeader ) )
		{
			WriteLine( "// Precompiled Header (pch in def)" );
			WriteLine( $"#include \"{definitions.PrecompiledHeader}\"" );
			WriteLine();
		}

		WriteLine( $"#include \"{headerName}\"" );

		foreach ( string inc in definitions.CppIncludes.Distinct() )
		{
			WriteLine( $"#include \"{inc}\"" );
		}

		if ( definitions.InitFrom == "Native" )
		{
			WriteLine( $"#include \"sbox/inetruntime.h\"" );
			WriteLine( $"#include \"tier0/managedhandle.h\"" );
			WriteLine( $"#include <string>" );
		}
		WriteLine();

		WriteLine( "" );
		WriteLine( "#pragma warning(disable : 4714)" );
		WriteLine( "#ifdef _WIN32" );
		WriteLine( "#define CC __stdcall" );
		WriteLine( "#else" );
		WriteLine( "#define CC" );
		WriteLine( "#endif" );

		{
			WriteLine( "//" );
			WriteLine( "// For instances where we'd otherwise be returning a pointer to a local" );
			WriteLine( "//" );
			WriteLine( "static thread_local CUtlString _sfstr;" );
			StartBlock( "const char* SafeReturnString( const char* input )" );
			{
				WriteLine( "if ( input == nullptr ) return nullptr;" );
				WriteLine( "_sfstr.Set( input );" );
				WriteLine( "return _sfstr.Get();" );
			}
			EndBlock();

			WriteLine( "static thread_local std::wstring _wstr;" );
			StartBlock( "const wchar_t *SafeReturnWString( const wchar_t *input )" );
			{
				WriteLine( "if ( input == nullptr ) return nullptr;" );
				WriteLine( "_wstr.assign( input );" );
				WriteLine( "return _wstr.c_str();" );
			}
			EndBlock();
		}

		if ( definitions.Externs.Count > 0 )
		{
			WriteLine( "" );
			foreach ( string inc in definitions.Externs.Distinct() )
			{
				WriteLine( $"extern {inc}" );
			}
		}

		Imports();

		Exports();

		Initialize();

		WriteLine( "" );
	}


	private void Initialize()
	{
		int exports = definitions.Classes.Where( x => x.Native == true ).Sum( x => x.Functions.Count );
		IEnumerable<Function> imports = definitions.Classes.Where( x => x.Native == false ).Where( x => !ShouldSkip( x ) ).SelectMany( x => x.Functions );

		WriteLine( "//" );
		WriteLine( "// MANAGER" );
		WriteLine( "// " );
		WriteLine( "// Manager class to set everything up" );
		WriteLine( "//" );

		foreach ( string ns in definitions.ManagedNamespace.Split( "." ) )
		{
			StartBlock( $"namespace {ns}" );
		}

		{
			StartBlock( "void Debug_Error( const char* string )" );
			{
				WriteLine( $"Plat_FatalError( \"{definitions.Ident} Failed To Initialize - %s\", string );" );
				WriteLine( "exit( 575 );" );
			}
			EndBlock();

			WriteLine( "" );
			WriteLine( $"typedef void (CC *fn_initialize)( const char*, int, void*, int*, void* );" );
			WriteLine( "" );


			WriteLine( "bool s_isReady = false;" );
			WriteLine( "" );

			StartBlock( $"DLL_EXPORT void CC igen_{definitions.Ident}( int hash, void** managedFunctions, void** nativeFunctions, int* structSizes )" );
			{
				StartBlock( $"if ( hash != {definitions.Hash} )" );
				{
					WriteLine( "Plat_FatalError( \"Invalid hash in x\" );" );
				}
				EndBlock();

				int i = 0;
				WriteLine( "" );
				{
					WriteLine( $"nativeFunctions[{i++}] = (void*)&Debug_Error;" );

					foreach ( Class c in definitions.Classes.Where( x => x.Native == true ) )
					{
						if ( ShouldSkip( c ) )
						{
							continue;
						}

						Class bc = c.BaseClass;

						while ( bc != null )
						{
							Class subclass = bc;
							WriteLine( $"nativeFunctions[{i++}] = (void*)&Exports::From_{subclass.ManagedName}_To_{c.ManagedName};" );
							WriteLine( $"nativeFunctions[{i++}] = (void*)&Exports::To_{subclass.ManagedName}_From_{c.ManagedName};" );
							bc = bc.BaseClass;
						}

						foreach ( Function f in c.Functions )
						{
							WriteLine( $"nativeFunctions[{i++}] = (void*)&Exports::{f.MangledName};" );
						}

						foreach ( Variable f in c.Variables )
						{
							WriteLine( $"nativeFunctions[{i++}] = (void*)&Exports::_Get__{f.MangledName};" );
							WriteLine( $"nativeFunctions[{i++}] = (void*)&Exports::_Set__{f.MangledName};" );
						}
					}
				}
				WriteLine( "" );

				i = 0;
				if ( definitions.Structs.Count > 0 )
				{
					foreach ( Struct s in definitions.Structs )
					{
						if ( ShouldSkip( s ) )
						{
							continue;
						}

						WriteLine( $"if ( sizeof( {s.NativeNameWithNamespace} ) != structSizes[{i}] ) Plat_FatalError( \"{s.NativeNameWithNamespace} is the wrong size\" );" );

						if ( !s.IsEnum && !s.HasAttribute( "small" ) )
						{
							WriteLine( $"static_assert ( sizeof( {s.NativeNameWithNamespace} ) >= 8, \"Please mark struct {s.NativeName} with a [small] - it's smaller than a pointer\" );" );
						}

						i++;
					}
				}

				WriteLine();

				if ( imports.Count() > 0 )
				{
					WriteLine( "" );
					WriteLine( $"// Not ready, failed, if any of the imports are null" );
					WriteLine( $"for ( int f =0; f<{imports.Count()}; f++ ) if ( managedFunctions[f] == nullptr ) Plat_FatalError( \"Huuuuuh\" );" );
				}


				WriteLine( "" );

				i = 0;
				foreach ( Function f in imports )
				{
					Class c = f.Class;
					IEnumerable<string> nativeArgs = c.SelfArg( true, f.Static ).Concat( f.Parameters ).Select( x => $"{x.GetNativeDelegateType( false )}" );
					string nativeArgS = string.Join( ",", nativeArgs );

					string functionType = $"{f.Return.GetNativeDelegateType( false )} (CC *)( {nativeArgS.Trim( ',', ' ' )} )";

					WriteLine( $"Imports::{f.MangledName} = ({functionType}) managedFunctions[{i}];" );

					i++;
				}



				WriteLine();
				WriteLine( "s_isReady = true;" );


			}
			EndBlock();

			WriteLine( "" );

			WriteLine();

			StartBlock( "bool IsReady()" );
			{
				WriteLine( "return s_isReady;" );
			}
			EndBlock();

			WriteLine( "" );

			StartBlock( "void Shutdown()" );
			{
				WriteLine( "s_isReady = false;" );
			}
			EndBlock();

		}
		foreach ( string ns in definitions.ManagedNamespace.Split( "." ) )
		{
			EndBlock();
		}
	}

}
