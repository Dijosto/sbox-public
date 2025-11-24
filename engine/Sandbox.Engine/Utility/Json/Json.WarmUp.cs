using System.Diagnostics;
using System.Text.Json;
using Sandbox.Internal;

namespace Sandbox;

partial class Json
{
	/// <summary>
	/// Try to do any reflection / code gen immediately, so we don't do anything too slow during gameplay.
	/// </summary>
	internal static void PopulateReflectionCache( TypeLibrary typeLibrary )
	{
		var jsonOptions = options;

		var sw = Stopwatch.StartNew();

		// Force the JsonSerializerOptions to become immutable JsonTypeInfos get cached

		_ = JsonSerializer.Deserialize<string>( "null", jsonOptions );

		// Try to create as many JsonTypeInfos as possible
		// This can be a background task, its expensive and we dont need to be populated immediately
		_ = Task.Run( () =>
		{
			foreach ( var typeDesc in typeLibrary.GetTypes() )
			{
				// ref structs & ptrs are invalid for serialization
				if ( typeDesc.TargetType.IsByRef || typeDesc.TargetType.IsPointer || typeDesc.TargetType.IsByRefLike )
					continue;

				// as are generics (with no target type)
				if ( typeDesc.TargetType.IsGenericType )
					continue;

				try
				{
					_ = jsonOptions.GetTypeInfo( typeDesc.TargetType );
				}
				catch
				{
					// Ignore not supported
				}
			}
		} );

		Log.Trace( $"Took {sw.Elapsed.TotalMilliseconds:F2}ms populating reflection cache" );
	}
}

