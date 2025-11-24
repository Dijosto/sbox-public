using NativeEngine;
using System.Runtime.InteropServices;

namespace Sandbox;

public enum AppSystemFlags
{
	None = 0,
	IsConsoleApp = 1 << 0,
	IsGameApp = 1 << 1,
	IsDedicatedServer = 1 << 2,
	IsStandaloneGame = 1 << 3,
	IsEditor = 1 << 4,
	IsUnitTest = 1 << 5,
}

public struct AppSystemCreateInfo
{
	public AppSystemFlags Flags;
	public string WindowTitle;

	internal MaterialSystem2AppSystemDictCreateInfo ToMaterialSystem2AppSystemDictCreateInfo()
	{
		var ci = new MaterialSystem2AppSystemDictCreateInfo
		{
			iFlags = (MaterialSystem2AppSystemDictFlags)Flags,
		};

		if ( !string.IsNullOrEmpty( WindowTitle ) )
		{
			ci.pWindowTitle = Marshal.StringToHGlobalAnsi( WindowTitle );
		}

		return ci;
	}
}
