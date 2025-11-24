using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.InteropGen;

internal partial class NativeWriter
{
	private void Imports()
	{
		WriteLine( "//" );
		WriteLine( "// IMPORTS" );
		WriteLine( "// " );
		WriteLine( "// Functions that we're getting from managed" );
		WriteLine( "//" );
		StartBlock( "namespace Imports" );
		{
			foreach ( Class c in definitions.Classes.Where( x => x.Native == false ) )
			{
				if ( ShouldSkip( c ) )
				{
					continue;
				}

				foreach ( Function f in c.Functions )
				{
					IEnumerable<string> nativeArgs = c.SelfArg( true, f.Static ).Concat( f.Parameters ).Where( x => x.IsRealArgument ).Select( x => $"{x.GetNativeDelegateType( false )} {x.Name}" );
					string nativeArgS = string.Join( ", ", nativeArgs );

					WriteLine( $"{f.Return.GetNativeDelegateType( false )} (CC *{f.MangledName})( {nativeArgS.Trim( ',', ' ' )} ) = nullptr;" );
				}
			}
		}
		EndBlock();

		ImportImplementations();


		WriteLine();
	}

	private void ImportImplementations()
	{
		WriteLine( "//" );
		WriteLine( "// IMPORT IMPLEMENTATIONS" );
		WriteLine( "//" );
		foreach ( Class c in definitions.Classes.Where( x => x.Native == false ).OrderByDescending( x => x.ClassDepth ) )
		{
			if ( ShouldSkip( c ) )
			{
				continue;
			}

			WriteLine( "// " );
			WriteLine( $"// {c.ManagedNameWithNamespace}" );
			WriteLine( "// " );

			if ( !c.Native && !c.Static )
			{
				foreach ( string ns in c.NativeNamespace.Split( ":", StringSplitOptions.RemoveEmptyEntries ) )
				{
					StartBlock( $"namespace {ns}" );
				}
			}

			foreach ( Function f in c.Functions )
			{
				IEnumerable<string> nativeArgs = f.Parameters.Select( x => $"{x.NativeType} {x.Name}" );
				string nativeArgS = string.Join( ", ", nativeArgs );
				string conststr = " const";

				if ( c.Static || f.Static )
				{
					conststr = "";
				}

				Write( $"{f.Return.NativeType} {c.NativeNameWithNamespace}::{f.Name}( {nativeArgS} ){conststr}{{ ", true );
				{
					IEnumerable<string> interopArgs = c.SelfArg( true, f.Static ).Concat( f.Parameters ).Select( x => x.ToInterop( true ) );
					string interopArgsS = string.Join( ", ", interopArgs );

					string func = $"Imports::{f.MangledName}( {interopArgsS} ) ";

					if ( f.HasReturn )
					{
						func = f.Return.FromInterop( true, func );
						func = f.Return.ReturnWrapCall( func, true );
					}
					else
					{
						func += ";";
					}

					Write( $"CALL_FROM_MANAGED_START(); " );

					Write( func );

					Write( $" CALL_FROM_MANAGED_END();" );
				}
				Write( " }" );
				WriteLine( "" );
			}

			if ( !c.Native && !c.Static )
			{
				foreach ( string ns in c.NativeNamespace.Split( ":", StringSplitOptions.RemoveEmptyEntries ) )
				{
					EndBlock();
				}
			}

			WriteLine();
		}
	}
}
