using Sandbox;
using System;

namespace Editor;

public partial class DockManager
{
	internal enum Area
	{
		//! The area tool windows has been added to most recently.
		LastUsedArea,
		//! New area in a detached window.
		NewFloatingArea,
		//! Area inside the manager widget (only available when there is no tool windows in it).
		EmptySpace,
		//! Tool window is hidden.
		NoArea,
		//! Existing area specified in AreaReference argument.
		AddTo,
		//! New area to the left of the area specified in AreaReference argument.
		LeftOf,
		//! New area to the right of the area specified in AreaReference argument.
		RightOf,
		//! New area to the top of the area specified in AreaReference argument.
		TopOf,
		//! New area to the bottom of the area specified in AreaReference argument.
		BottomOf,
		//! New area to the left of the window containing the specified in AreaReference argument.
		LeftWindowSide = 9,
		//! New area to the right of the window containing the specified in AreaReference argument.
		RightWindowSide = 10,
		//! New area to the top of the window containing the specified in AreaReference argument.
		TopWindowSide = 11,
		//! New area to the bottom of the window containing the specified in AreaReference argument.
		BottomWindowSide = 12,
		//! Invalid value, just indicates the number of types available
		NumReferenceTypes
	};
}
