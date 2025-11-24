
namespace Sandbox.UI;

public static class Clipboard
{
	/// <summary>
	/// Sets the clipboard text
	/// </summary>
	public static void SetText( string text )
	{
		NativeEngine.EngineGlobal.Plat_SetClipboardText( text );
	}
}
