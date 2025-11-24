using System.Runtime.InteropServices;

namespace Sandbox;

/// <summary>
/// Draws a vertex object. Lines or solids usually.
/// </summary>
internal class VertexSceneObject : SceneDynamicObject
{
	public List<Vertex> Vertices { get; }
	public Graphics.PrimitiveType PrimitiveType { get; set; }

	public VertexSceneObject( SceneWorld sceneWorld ) : base( sceneWorld )
	{
		RenderLayer = SceneRenderLayer.OverlayWithoutDepth;
		Vertices = new List<Vertex>();
	}

	public void Write()
	{
		if ( Vertices.Count == 0 )
			return;

		Init( PrimitiveType );

		AddVertex( CollectionsMarshal.AsSpan( Vertices ) );

		// Clear our vertices queue after we upload to the GPU
		Vertices.Clear();
	}
}
