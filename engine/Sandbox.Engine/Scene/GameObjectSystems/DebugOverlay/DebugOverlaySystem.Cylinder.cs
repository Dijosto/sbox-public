namespace Sandbox;

public partial class DebugOverlaySystem
{
	/// <summary>
	/// Draw a wireframe cylinder, like a capsule without the hemispheres, showing all sides.
	/// </summary>
	public void Cylinder( Capsule capsule, Color color = default, float duration = 0, Transform transform = default, bool overlay = false, int segments = 12 )
	{
		if ( transform == default ) transform = Transform.Zero;
		if ( color == default ) color = Color.White;

		var sceneObject = new SceneDynamicObject( Scene.SceneWorld )
		{
			Transform = transform,
			Material = LineMaterial,
			RenderLayer = overlay ? SceneRenderLayer.OverlayWithoutDepth : SceneRenderLayer.OverlayWithDepth
		};

		sceneObject.Flags.CastShadows = false;
		sceneObject.Init( Graphics.PrimitiveType.Lines );

		var axis = capsule.CenterB - capsule.CenterA;
		var direction = axis.Length > 0 ? axis.Normal : Vector3.Up;

		var tangent = Vector3.Cross( direction, Vector3.Right );
		if ( tangent.IsNearZeroLength ) tangent = Vector3.Cross( direction, Vector3.Up );
		tangent = tangent.Normal;

		var bitangent = Vector3.Cross( direction, tangent ).Normal;
		var angleStep = MathF.Tau / segments;

		for ( int i = 0; i < segments; i++ )
		{
			var angleStart = i * angleStep;
			var angleEnd = (i + 1) * angleStep;

			var startRingBottom = capsule.CenterA + (tangent * MathF.Cos( angleStart ) + bitangent * MathF.Sin( angleStart )) * capsule.Radius;
			var endRingBottom = capsule.CenterA + (tangent * MathF.Cos( angleEnd ) + bitangent * MathF.Sin( angleEnd )) * capsule.Radius;
			var startRingTop = capsule.CenterB + (tangent * MathF.Cos( angleStart ) + bitangent * MathF.Sin( angleStart )) * capsule.Radius;
			var endRingTop = capsule.CenterB + (tangent * MathF.Cos( angleEnd ) + bitangent * MathF.Sin( angleEnd )) * capsule.Radius;

			sceneObject.AddVertex( new Vertex( startRingBottom, color ) );
			sceneObject.AddVertex( new Vertex( endRingBottom, color ) );
			sceneObject.AddVertex( new Vertex( startRingTop, color ) );
			sceneObject.AddVertex( new Vertex( endRingTop, color ) );
			sceneObject.AddVertex( new Vertex( startRingBottom, color ) );
			sceneObject.AddVertex( new Vertex( startRingTop, color ) );
		}

		Add( duration, sceneObject );
	}
}
