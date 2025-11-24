using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sandbox.Generator;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Sandbox;

partial class Compiler
{
	private void RunBlacklistWalker( CSharpCompilation compiler, CompilerOutput output )
	{
		if ( !compiler.SyntaxTrees.Any() )
		{
			return;
		}

		ConcurrentBag<Diagnostic> diagnostics = new();

		var result = System.Threading.Tasks.Parallel.ForEach( compiler.SyntaxTrees, tree =>
		{
			var semanticModel = compiler.GetSemanticModel( tree );

			var walker = new BlacklistCodeWalker( semanticModel );
			walker.Visit( tree.GetRoot() );

			walker.Diagnostics.ForEach( diagnostics.Add );
		} );

		output.Diagnostics.AddRange( diagnostics );
	}
}
