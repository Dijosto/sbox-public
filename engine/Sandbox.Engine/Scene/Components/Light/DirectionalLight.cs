namespace Sandbox;

/// <summary>
/// A directional light that casts shadows, like the sun.
/// </summary>
[Expose]
[Title( "Directional Light" )]
[Category( "Light" )]
[Icon( "light_mode" )]
[EditorHandle( "materials/gizmo/directionallight.png" )]
[Alias( "DirectionalLightComponent" )]
public class DirectionalLight : Light
{
	/// <summary>
	/// Color of the ambient sky color
	/// This is kept for long term support, the recommended way to do this is with an Ambient Light component.
	/// </summary>
	[Property]
	public Color SkyColor { get; set; }

	public DirectionalLight()
	{
		LightColor = "#E9FAFF";
	}

	protected override SceneLight CreateSceneObject()
	{
		var o = new SceneDirectionalLight( Scene.SceneWorld, WorldRotation, LightColor );
		return o;
	}

	protected override void OnAwake()
	{
		Tags.Add( "light_directional" );

		base.OnAwake();
	}

	protected override void UpdateSceneObject( SceneLight l )
	{
		base.UpdateSceneObject( l );

		if ( l is SceneDirectionalLight o )
		{
			o.ShadowCascadeCount = 3;
		}
	}
	protected override void DrawGizmos()
	{
		using var scope = Gizmo.Scope( $"light-{GetHashCode()}" );

		var fwd = Vector3.Forward;

		Gizmo.Draw.Color = LightColor;

		for ( float f = 0; f < MathF.PI * 2; f += 0.5f )
		{
			var x = MathF.Sin( f );
			var y = MathF.Cos( f );

			var off = (x * Vector3.Left + y * Vector3.Up) * 5.0f;

			Gizmo.Draw.Line( off, off + fwd * 30 );
		}

		//	Gizmo.Transform = Transform.Zero;
		//	Gizmo.Draw.Sprite( GameObject.Transform.Position, 10, Texture.Load( FileSystem.Mounted, "/editor/directional_light.png" ) );

	}
}
