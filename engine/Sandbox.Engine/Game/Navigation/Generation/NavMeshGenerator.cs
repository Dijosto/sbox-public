namespace Sandbox.Navigation.Generation;

[SkipHotload]
class NavMeshGenerator : IDisposable
{
	// Created in init disposed of after generate
	private CompactHeightfield chfWorkingCopy;

	private Config cfg;

	public void Init( Config config, CompactHeightfield inputChf )
	{
		cfg = config;
		if ( chfWorkingCopy == null )
		{
			chfWorkingCopy = inputChf.Copy();
		}
		else
		{
			// Reuse memory
			inputChf.CopyTo( chfWorkingCopy );
		}

		if ( contourSet == null )
		{
			contourSet = new ContourSet();
		}
		else
		{
			contourSet.Clear();
		}
	}

	public void Dispose()
	{
		if ( chfWorkingCopy != null )
		{
			chfWorkingCopy.Dispose();
			chfWorkingCopy = null;
		}
		if ( contourSet != null )
		{
			contourSet.Dispose();
			contourSet = null;
		}
	}

	public void MarkArea( NavMeshAreaData area, int areaId )
	{
		var navTransform = NavMesh.ToNav( area.Transform );
		var navBounds = NavMesh.ToNav( area.LocalBounds ).Transform( navTransform );

		if ( area.Volume.Type == Volumes.SceneVolume.VolumeTypes.Box )
		{
			var navBox = NavMesh.ToNav( area.Volume.Box );
			chfWorkingCopy.MarkBoxArea( navBox, navTransform, navBounds, areaId );
		}
		else if ( area.Volume.Type == Volumes.SceneVolume.VolumeTypes.Capsule )
		{
			var navCapsule = NavMesh.ToNav( area.Volume.Capsule );
			chfWorkingCopy.MarkCapsuleArea( navCapsule, navTransform, navBounds, areaId );
		}
		else if ( area.Volume.Type == Volumes.SceneVolume.VolumeTypes.Sphere )
		{
			var navSpehere = NavMesh.ToNav( area.Volume.Sphere );
			chfWorkingCopy.MarkSphereArea( navSpehere, navTransform, navBounds, areaId );
		}
		else if ( area.Volume.Type == Volumes.SceneVolume.VolumeTypes.Infinite )
		{
			var infiniteBounds = new BBox( new Vector3( float.MinValue ), new Vector3( float.MaxValue ) );
			chfWorkingCopy.MarkBoxArea( infiniteBounds, Transform.Zero, infiniteBounds, areaId );
		}
	}

	private List<int> vertsCache = new( 256 );
	private List<int> vertsSimplifiedCache = new( 128 );
	private List<int> prevCache = new( 512 );
	private ContourSet contourSet;

	public PolyMesh Generate()
	{
		// According to recast docs good for tiles
		if ( !RegionBuilder.BuildLayerRegions( chfWorkingCopy, cfg.BorderSize, cfg.MinRegionArea, prevCache ) )
		{
			//Log.Warning( "buildNavigation: Could not build layer regions.\n" );
			return null;
		}

		//
		// Step 5. Trace and simplify region contours.
		//

		// Create contours.
		ContourBuilder.BuildContours( chfWorkingCopy, cfg.MaxSimplificationError, cfg.MaxEdgeLen, vertsCache, vertsSimplifiedCache, contourSet );

		if ( contourSet.Contours.Count == 0 )
		{
			//Log.Warning( "buildNavigation: No contours could be build for regions.\n" );

			return null;
		}

		//
		// Step 6. Build polygons mesh from contours.
		//

		// Build polygon navmesh from the contours.
		return PolyMeshBuilder.BuildPolyMesh( contourSet, cfg.MaxVertsPerPoly );
	}
}
