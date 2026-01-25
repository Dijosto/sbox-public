using Mono.Cecil;

namespace Sandbox.Obfuscation.Transforms;

/// <summary>
/// Interface for obfuscation transforms that can be applied to an assembly.
/// </summary>
public interface IObfuscationTransform
{
	/// <summary>
	/// Human-readable name of this transform.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Apply the transform to the assembly.
	/// </summary>
	/// <param name="assembly">The assembly to transform.</param>
	/// <param name="preservation">Preservation analysis results indicating what not to change.</param>
	/// <param name="context">Shared context for transforms.</param>
	void Apply( AssemblyDefinition assembly, PreservationResult preservation, ObfuscationContext context );
}

/// <summary>
/// Shared context passed between obfuscation transforms.
/// </summary>
public class ObfuscationContext
{
	/// <summary>
	/// The obfuscation configuration.
	/// </summary>
	public ObfuscationConfig Config { get; }

	/// <summary>
	/// Shared name generator for consistent naming.
	/// </summary>
	public NameGenerator NameGenerator { get; }

	/// <summary>
	/// Mapping of original names to obfuscated names.
	/// Key: Original full name, Value: Obfuscated name
	/// </summary>
	public Dictionary<string, string> NameMap { get; } = new();

	/// <summary>
	/// Mapping of original type names to their new TypeDefinitions.
	/// </summary>
	public Dictionary<string, TypeDefinition> TypeMap { get; } = new();

	/// <summary>
	/// Log messages generated during obfuscation.
	/// </summary>
	public List<ObfuscationLogEntry> Log { get; } = new();

	/// <summary>
	/// Statistics about the obfuscation process.
	/// </summary>
	public ObfuscationStats Stats { get; } = new();

	public ObfuscationContext( ObfuscationConfig config )
	{
		Config = config;
		NameGenerator = new NameGenerator( config.Seed );
	}

	public void LogInfo( string message )
	{
		Log.Add( new ObfuscationLogEntry( ObfuscationLogLevel.Info, message ) );
	}

	public void LogWarning( string message )
	{
		Log.Add( new ObfuscationLogEntry( ObfuscationLogLevel.Warning, message ) );
	}

	public void LogError( string message )
	{
		Log.Add( new ObfuscationLogEntry( ObfuscationLogLevel.Error, message ) );
	}
}

public enum ObfuscationLogLevel
{
	Info,
	Warning,
	Error
}

public record ObfuscationLogEntry( ObfuscationLogLevel Level, string Message );

/// <summary>
/// Statistics about the obfuscation process.
/// </summary>
public class ObfuscationStats
{
	public int TypesRenamed { get; set; }
	public int MethodsRenamed { get; set; }
	public int FieldsRenamed { get; set; }
	public int PropertiesRenamed { get; set; }
	public int EventsRenamed { get; set; }
	public int ParametersRenamed { get; set; }
	public int StringsEncrypted { get; set; }
	public int MethodsControlFlowObfuscated { get; set; }
	public int AttributesRemoved { get; set; }

	public int TotalRenamed => TypesRenamed + MethodsRenamed + FieldsRenamed +
	                          PropertiesRenamed + EventsRenamed + ParametersRenamed;

	public override string ToString()
	{
		return $"Renamed: {TotalRenamed} symbols ({TypesRenamed} types, {MethodsRenamed} methods, " +
		       $"{FieldsRenamed} fields, {PropertiesRenamed} properties, {EventsRenamed} events, " +
		       $"{ParametersRenamed} parameters), {StringsEncrypted} strings encrypted, " +
		       $"{MethodsControlFlowObfuscated} methods control-flow obfuscated, " +
		       $"{AttributesRemoved} attributes removed";
	}
}
