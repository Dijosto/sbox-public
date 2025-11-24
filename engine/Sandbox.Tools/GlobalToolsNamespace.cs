using System.Text.Json;

namespace Sandbox.Internal;

public static class GlobalToolsNamespace
{
	//public static Logger Log { get; } = new Logger( "tools" );
	public static Editor.EditorMainWindow EditorWindow { get; internal set; }
	public static CookieContainer EditorCookie { get; internal set; }
	public static CookieContainer ProjectCookie { get; internal set; }
	public static Sandbox.Bind.BindSystem BindSystem { get; internal set; }
	//public static HashSet<IPanel> RootPanels => IPanel.GetAllRootPanels();
	//public static AudioSystem Audio { get; internal set; } = new AudioSystem( false );
	public static TypeLibrary EditorTypeLibrary { get; internal set; }
	//public static ResourceLibrary ResourceLibrary { get; internal set; } = new( Context.Tools );
	public static Facepunch.ActionGraphs.NodeLibrary EditorNodeLibrary => Game.NodeLibrary;
	public static JsonSerializerOptions EditorJsonOptions => Json.options;
}
