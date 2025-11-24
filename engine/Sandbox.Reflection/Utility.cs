namespace Sandbox;

internal static class ReflectionUtility
{
	public static void RunAllStaticConstructors( string assemblyName )
	{
		var asm = Assembly.Load( assemblyName );
		RunAllStaticConstructors( asm );
	}

	public static void RunAllStaticConstructors( Assembly asm )
	{
		List<Exception> exceptions = null;

		foreach ( var t in asm.GetTypes() )
		{
			try
			{
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor( t.TypeHandle );
			}
			catch ( Exception ex )
			{
				exceptions ??= new List<Exception>();
				exceptions.Add( ex );
			}
		}

		if ( exceptions is not { Count: > 0 } ) return;

		throw exceptions.Count == 1 ? exceptions[0] : new AggregateException( exceptions );
	}

	/// <summary>
	/// Pre-compile all of the methods that we can, to reduce the risk of them compiling during gameplay
	/// </summary>
	public static void PreJIT( Assembly asm )
	{
		foreach ( var t in asm.GetTypes() )
		{
			if ( t.IsGenericTypeDefinition ) continue;

			foreach ( var method in t.GetMethods( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance ) )
			{
				if ( method.IsAbstract ) continue;
				if ( method.ContainsGenericParameters ) continue;
				if ( method.DeclaringType?.BaseType == typeof( MulticastDelegate ) ) continue;
				if ( method.Name is "BeginInvoke" or "EndInvoke" ) continue; // Skip async delegate methods

				try
				{
					System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod( method.MethodHandle );
				}
				catch ( System.Exception e )
				{
					Logging.GetLogger( "PreJit" ).Warning( e, $"{e.Message} - {t}.{method}" );
				}
			}

			foreach ( var method in t.GetConstructors( BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance ) )
			{
				if ( method.IsAbstract ) continue;
				if ( method.ContainsGenericParameters ) continue;
				if ( method.DeclaringType?.BaseType == typeof( MulticastDelegate ) ) continue;
				if ( method.Name is "BeginInvoke" or "EndInvoke" ) continue; // Skip async delegate methods

				try
				{
					System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod( method.MethodHandle );
				}
				catch ( System.Exception e )
				{
					Logging.GetLogger( "PreJit" ).Warning( e, $"{e.Message} - {t}.{method}" );
				}
			}
		}
	}
}
