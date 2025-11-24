namespace Sandbox.Engine.Shaders;

/// <summary>
/// Options used when compiling a shader
/// </summary>
public struct ShaderCompileOptions
{
	public bool SingleThreaded { get; set; }
	public bool ForceRecompile { get; set; }

	/// <summary>
	/// Write to console. Used when running from the command line.
	/// </summary>
	public bool ConsoleOutput { get; set; }
}
