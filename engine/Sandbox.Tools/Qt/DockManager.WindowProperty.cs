using Sandbox;
using System;

namespace Editor;

public partial class DockManager
{
	[Flags]
	public enum DockProperty
	{
		/// <summary>
		/// Disables all drag/docking ability by the user
		/// </summary>
		DisallowUserDocking = 0x1,

		/// <summary>
		/// Hides the close button on the tab for this tool window
		/// </summary>
		HideCloseButton = 0x2,

		/// <summary>
		/// Disable the user being able to drag this tab in the tab bar, to rearrange
		/// </summary>
		DisableDraggableTab = 0x4,

		/// <summary>
		/// When the tool window is closed, hide it instead of removing it
		/// </summary>
		HideOnClose = 0x8,

		/// <summary>
		/// Don't allow this tool window to be floated
		/// </summary>
		DisallowFloatWindow = 0x10,

		/// <summary>
		/// When displaying this tool window in tabs, always display the tabs even if there's only one
		/// </summary>
		AlwaysDisplayFullTabs = 0x20,
	};
}
