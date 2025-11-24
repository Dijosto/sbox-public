namespace DotRecast.Detour
{
	/// Options for dtNavMeshQuery::findStraightPath.
	internal static class DtStraightPathOptions
	{
		public const int DT_STRAIGHTPATH_AREA_CROSSINGS = 0x01; //< Add a vertex at every polygon edge crossing where area changes.
		public const int DT_STRAIGHTPATH_ALL_CROSSINGS = 0x02; //< Add a vertex at every polygon edge crossing. 
	}

	internal class DtStraightPathOption
	{
		public static readonly DtStraightPathOption None = new DtStraightPathOption( 0, "None" );
		public static readonly DtStraightPathOption AreaCrossings = new DtStraightPathOption( DtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS, "Area" );
		public static readonly DtStraightPathOption AllCrossings = new DtStraightPathOption( DtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS, "All" );

		public readonly int Value;
		public readonly string Label;

		private DtStraightPathOption( int value, string label )
		{
			Value = value;
			Label = label;
		}
	}
}
