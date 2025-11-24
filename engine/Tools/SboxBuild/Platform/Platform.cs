using System.Runtime.InteropServices;

namespace Facepunch;

/// <summary>
/// Base functionality shared across all platforms
/// </summary>
public abstract class Platform
{
	protected abstract string PlatformBaseName { get; }

	/// <returns>Full platform ID like "win64", "osxarm64", etc.</returns>
	public string PlatformID => GetPlatformID( PlatformBaseName );

	public abstract bool CompileSolution( string solutionName, bool forceRebuild = false );

	/// <summary>
	/// Creates the appropriate platform implementation for the current OS
	/// </summary>
	public static Platform Create()
	{
		if ( OperatingSystem.IsWindows() )
		{
			return new WindowsPlatform();
		}
		else if ( OperatingSystem.IsLinux() )
		{
			return new LinuxPlatform();
		}
		else if ( OperatingSystem.IsMacOS() )
		{
			return new MacOSPlatform();
		}

		throw new PlatformNotSupportedException( $"Unsupported platform: {RuntimeInformation.OSDescription}" );
	}

	/// <summary>
	/// Builds a platform ID by combining the base platform name with the current architecture
	/// </summary>
	/// <param name="basePlatformName">Base platform name like "win", "osx", "linuxsteamrt"</param>
	/// <returns>Full platform ID like "win64", "osxarm64", etc.</returns>
	protected static string GetPlatformID( string basePlatformName )
	{
		var arch = RuntimeInformation.ProcessArchitecture;
		string architecture = arch switch
		{
			Architecture.X64 => "64",
			Architecture.Arm64 => "arm64",
			Architecture.X86 => "32",
			Architecture.Arm => "arm",
			_ => "unknown"
		};

		return basePlatformName + architecture;
	}
}
