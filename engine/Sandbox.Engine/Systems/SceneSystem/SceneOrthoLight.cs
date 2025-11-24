
using NativeEngine;

namespace Sandbox;

/// <summary>
/// Keep this internal for now
/// </summary>
internal class SceneOrthoLight : SceneLight
{
	public SceneOrthoLight( SceneWorld world ) : base()
	{
		Assert.IsValid( world );

		using ( var h = IHandle.MakeNextHandle( this ) )
		{
			CSceneSystem.CreateOrthoLight( world );
		}
	}
}
