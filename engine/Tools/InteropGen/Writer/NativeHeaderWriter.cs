using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.InteropGen;

internal class NativeHeaderWriter : BaseWriter
{
	public NativeHeaderWriter( Definition definitions, string targetName ) : base( definitions, targetName )
	{
	}

	public override void Generate()
	{
		string HEADER_DEF = "_" + definitions.Filename.Replace( ".def", "" ).Replace( ".", "_" ).ToUpper() + "_H";

		WriteLine( $"#ifndef {HEADER_DEF}" );
		WriteLine( $"#define {HEADER_DEF}" );
		WriteLine( $"#pragma once" );
		WriteLine( "" );

		foreach ( string inc in definitions.Includes.Distinct() )
		{
			if ( ShouldSkipInclude( inc, "native-header" ) )
			{
				continue;
			}

			WriteLine( $"#include \"{inc}\"" );
		}


		{

			WriteLine( "" );

			StartBlock( "namespace Sandbox" );
			WriteLine( "class IHost;" );
			EndBlock();

			WriteLine( "" );

			foreach ( string ns in definitions.ManagedNamespace.Split( "." ) )
			{
				StartBlock( $"namespace {ns}" );
			}

			{
				if ( definitions.InitFrom == "Native" )
				{
					//WriteLine( "//" );
					//WriteLine( "// Should be called once at startup to set up all of our function pointers etc" );
					//WriteLine( "//" );
					//WriteLine( "void SetupInterop( ::INetRuntime* host );" );
				}

				WriteLine( "" );
				WriteLine( "" );
				WriteLine( "//" );
				WriteLine( "// Will return true if all the binds are setup and ready to call" );
				WriteLine( "// Will return false after application shutdown." );
				WriteLine( "//" );
				WriteLine( "bool IsReady();" );
				WriteLine( "" );
				WriteLine( "//" );
				WriteLine( "// Will set ready to false, indicating that managed bindings are no longer available." );
				WriteLine( "// This is called after application shutdown." );
				WriteLine( "//" );
				WriteLine( "void Shutdown();" );
			}
			foreach ( string ns in definitions.ManagedNamespace.Split( "." ) )
			{
				EndBlock();
			}
		}

		Imports();

		WriteLine( "" );
		WriteLine( "#endif" );
	}

	private void Imports()
	{
		WriteLine( "//" );
		WriteLine( "// IMPORTS" );
		WriteLine( "// " );
		WriteLine( "// Functions that we're getting from managed" );
		WriteLine( "//" );
		WriteLine();

		foreach ( Class c in definitions.Classes.Where( x => x.Native == false ).OrderBy( x => x.NativeOrder( definitions.Classes ) ) )
		{
			if ( ShouldSkip( c, "native-header" ) )
			{
				continue;
			}

			StartSubFile();

			WriteLine( $"#pragma once" );
			WriteLine( $"" );

			foreach ( string ns in c.NativeNamespace.Split( ":", StringSplitOptions.RemoveEmptyEntries ) )
			{
				StartBlock( $"namespace {ns}" );
			}

			string baseclass = c.BaseClass != null ? $" : public {c.BaseClass.NativeNameWithNamespace}" : "";

			StartBlock( $"class {c.NativeName}{baseclass}" );
			{
				WriteLine( "public:" );
				Indent++;

				string sttic = "static ";
				string pv = "";

				if ( !c.Static )
				{
					sttic = "";
					pv = " const";

					WriteLine( $"{c.NativeName}() {{ m_ObjectId = 0;  }}" );
					WriteLine( $"{c.NativeName}( unsigned int id ) {{ m_ObjectId = id;  }}" );
					WriteLine( $"unsigned int m_ObjectId = 0;" );
					WriteLine( $"operator unsigned int() const {{ return m_ObjectId; }}" );
					WriteLine( $"unsigned int ptr(){{ return m_ObjectId; }}" );
					WriteLine( $"bool HasObject(){{ return m_ObjectId > 0; }}" );
				}

				foreach ( Function f in c.Functions )
				{
					sttic = (c.Static || f.Static) ? "static " : "";
					pv = (c.Static || f.Static) ? "" : " const";

					IEnumerable<string> nativeArgs = f.Parameters.Select( x => $"{x.NativeType} {x.Name}" );
					string nativeArgS = string.Join( ",", nativeArgs );

					WriteLine( $"{sttic}{f.Return.NativeType} {f.Name}( {nativeArgS} ){pv};" );
				}

				Indent--;
			}
			EndBlock( ";" );

			foreach ( string ns in c.NativeNamespace.Split( ":", StringSplitOptions.RemoveEmptyEntries ) )
			{
				EndBlock();
			}

			WriteLine();

			string fileOut = EndSubFile( c.NativeName.ToLower() );
			WriteLine( $"#include \"{fileOut}\"" );
		}
	}

}
