using Mono.Cecil;
using System;
using System.Collections.Concurrent;

namespace Sandbox;

public partial class AccessControl
{
	static ConcurrentDictionary<AssemblyNameReference, AssemblyDefinition> GlobalAssemblyCache = new( AssemblyNameComparer.Instance );

	public AssemblyDefinition Resolve( AssemblyNameReference name )
	{
		//
		// Look in the dynamic assemblies first
		//
		if ( Assemblies != null )
		{
			if ( Assemblies.TryGetValue( name, out var assm ) )
				return assm;

			var newestNameMatch = Assemblies
				.Where( x => x.Key.Name.Equals( name.Name, StringComparison.OrdinalIgnoreCase ) )
				//.Where( x => x.Key.Version.CompareTo( name.Version ) >= 0 )
				.OrderByDescending( x => x.Key.Version )
				.FirstOrDefault();

			if ( newestNameMatch.Value != null )
				return newestNameMatch.Value;
		}
		//
		// We only resolve certain named dlls from disk - and certainly not package.
		//
		if ( !name.Name.StartsWith( "Sandbox.", StringComparison.OrdinalIgnoreCase ) &&
			 !name.Name.StartsWith( "System.", StringComparison.OrdinalIgnoreCase ) &&
			 name.Name != "Microsoft.AspNetCore.Components" )
			throw NotResolved( name );

		//
		// Now look at our System. and Sandbox. assemblies
		//
		if ( GlobalAssemblyCache.TryGetValue( name, out var systemAssembly ) )
			return systemAssembly;

		lock ( GlobalAssemblyCache )
		{
			return GlobalAssemblyCache.GetOrAdd( name, FindAssemblyOnDisk );
		}
	}

	AssemblyDefinition FindAssemblyOnDisk( AssemblyNameReference name )
	{
		var dllName = $"{name.Name}.dll";

		var corePath = System.IO.Path.GetDirectoryName( typeof( Object ).Assembly.Location );
		var testPath = System.IO.Path.Combine( corePath, dllName );

		if ( !System.IO.File.Exists( testPath ) )
		{
			var gamePath = System.IO.Path.GetDirectoryName( GetType().Assembly.Location );
			testPath = System.IO.Path.Combine( gamePath, dllName );
		}

		if ( !System.IO.File.Exists( testPath ) )
			throw NotResolved( name );

		var fileContent = System.IO.File.ReadAllBytes( testPath );
		var stream = new MemoryStream( fileContent );

		var options = new ReaderParameters { ReadingMode = ReadingMode.Immediate, InMemory = true, AssemblyResolver = this };
		var assm = AssemblyDefinition.ReadAssembly( stream, options );

		return assm;
	}

	private Exception NotResolved( AssemblyNameReference name )
	{
		return new System.Exception( $"Couldn't resolve '{name}' [{string.Join( ";", Assemblies.Select( x => $"{x.Key}@{x.Key}" ) )}]" );
	}

	public AssemblyDefinition Resolve( AssemblyNameReference name, ReaderParameters parameters )
	{
		return Resolve( name );
	}
}
