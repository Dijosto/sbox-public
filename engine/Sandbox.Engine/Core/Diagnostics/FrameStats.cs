namespace Sandbox.Diagnostics;

/// <summary>
/// Stats returned from the engine each frame describing what was rendered, and how much of it.
/// </summary>
public struct FrameStats
{
	public static FrameStats Current => _current;
	internal static FrameStats _current = new();

	internal FrameStats( SceneSystemPerFrameStats_t stats )
	{
		ObjectsRendered = stats.m_nNumObjectsPassingCullCheck;
		TrianglesRendered = stats.m_nTrianglesRendered;
		DrawCalls = stats.m_nDrawCalls;
		MaterialChanges = stats.m_nMaterialChangesNonShadow;
		DisplayLists = stats.m_nNumDisplayListsSubmitted;
		SceneViewsRendered = stats.m_nNumViewsRendered;
		RenderTargetResolves = stats.m_nNumResolves;
		ObjectsCulledByVis = stats.m_nNumObjectsRejectedByVis;
		ObjectsCulledByScreenSize = stats.m_nNumObjectsRejectedByScreenSizeCulling;
		ObjectsCulledByFade = stats.m_nNumObjectsRejectedByFading;
		ObjectsFading = stats.m_nNumFadingObjects;
		ShadowedLightsInView = stats.m_nNumShadowedLightsInView;
		UnshadowedLightsInView = stats.m_nNumUnshadowedLightsInView;
		ShadowMaps = stats.m_nNumShadowMaps;
	}

	/// <summary>
	/// Number of objects rendered that passed the cull checks.
	/// </summary>
	public double ObjectsRendered { get; set; }

	/// <summary>
	/// Total number of triangles rendered
	/// </summary>
	public double TrianglesRendered { get; set; }

	/// <summary>
	/// Number of draw calls
	/// </summary>
	public double DrawCalls { get; set; }

	/// <summary>
	/// Number of scenesystem material changes
	/// </summary>
	public double MaterialChanges { get; set; }

	/// <summary>
	/// Number of display lists submitted to the GPU
	/// </summary>
	public double DisplayLists { get; set; }

	/// <summary>
	/// Number of scene system views rendered
	/// </summary>
	public double SceneViewsRendered { get; set; }

	/// <summary>
	/// Number of render target resolves
	/// </summary>
	public double RenderTargetResolves { get; set; }

	/// <summary>
	/// Number of objects culled by static visibility (vis)
	/// </summary>
	public double ObjectsCulledByVis { get; set; }

	/// <summary>
	/// Number of objects culled by screen size
	/// </summary>
	public double ObjectsCulledByScreenSize { get; set; }

	/// <summary>
	/// Number of objects culled by distance fading
	/// </summary>
	public double ObjectsCulledByFade { get; set; }

	/// <summary>
	/// Number of objects currently being distance-faded
	/// </summary>
	public double ObjectsFading { get; set; }

	/// <summary>
	/// Number of lights in view that cast shadows
	/// </summary>
	public double ShadowedLightsInView { get; set; }

	/// <summary>
	/// Number of lights in view that don't cast shadows
	/// </summary>
	public double UnshadowedLightsInView { get; set; }

	/// <summary>
	/// Number of shadow maps rendered this frame
	/// </summary>
	public double ShadowMaps { get; set; }
}
