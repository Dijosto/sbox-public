using DotRecast.Detour;
using DotRecast.Detour.Crowd;
using Sandbox.Engine.Resources;

namespace Sandbox.Navigation;

internal class NavMeshQueryFilter : IDtQueryFilter
{
	private NavMesh _nav;
	private NavMeshAgent _agent;

	public NavMeshQueryFilter( NavMesh nav, NavMeshAgent agent )
	{
		_nav = nav;
		_agent = agent;
	}

	public bool PassFilter( long refs )
	{
		_nav.navmeshInternal.GetTileAndPolyByRef( refs, out DtMeshTile _, out DtPoly curPoly );
		var areaDef = _nav.AreaIdToDefinition( curPoly.area );
		if ( areaDef == null ) return _agent.AllowDefaultArea;

		var allowedToPass = _agent.AllowedAreas.Count == 0 || _agent.AllowedAreas.Contains( areaDef );
		var notForbiddenToPass = _agent.ForbiddenAreas.Count == 0 || !_agent.ForbiddenAreas.Contains( areaDef );
		return allowedToPass && notForbiddenToPass;
	}

	public float GetCost( Vector3 pa, Vector3 pb, long prevRef, long curRef, long nextRef )
	{
		var distance = Vector3.DistanceBetween( pa, pb );

		_nav.navmeshInternal.GetTileAndPolyByRef( curRef, out DtMeshTile _, out DtPoly curPoly );

		var areaDef = _nav.AreaIdToDefinition( curPoly.area );
		float multiplier = areaDef?.CostMultiplier ?? 1f;

		return distance * multiplier;
	}
}
