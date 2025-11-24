namespace Editor
{
	public enum DockArea
	{
		LastUsed = DockManager.Area.LastUsedArea,

		Floating = DockManager.Area.NewFloatingArea,

		Hidden = DockManager.Area.NoArea,

		Inside = DockManager.Area.AddTo,

		Left = DockManager.Area.LeftOf,
		Right,
		Top,
		Bottom,

		LeftOuter = DockManager.Area.LeftWindowSide,
		RightOuter,
		TopOuter,
		BottomOuter,

	};
}
