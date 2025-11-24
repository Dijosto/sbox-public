using Facepunch.ActionGraphs;

namespace Sandbox.ActionGraphs;

/// <summary>
/// Handles drawing lines representing ActionGraph references between objects / components in the scene.
/// </summary>
internal class SceneRefGizmo
{
	private record struct NodeKey( Guid GraphId, int NodeId )
	{
		public static explicit operator NodeKey( Node node )
		{
			return new NodeKey( node.ActionGraph.Guid, node.Id );
		}
	}

	private record SceneRefNode( NodeKey NodeKey, WeakReference<Node> Node )
	{
		public TimeSince LastTriggered { get; set; }

		public GameObject ReferencedObject { get; set; }
		public Component ReferencedComponent { get; set; }

		public bool IsValid => Node.TryGetTarget( out var target ) && target.IsValid && ReferencedObject.IsValid();
	}

	private GameObject GameObject { get; }

	public SceneRefGizmo( GameObject gameObject )
	{
		GameObject = gameObject;
	}

	private readonly Dictionary<NodeKey, SceneRefNode> _sceneRefNodes = new();
	private readonly List<NodeKey> _lastTriggered = new();

	private const float MaxActionGraphLinkDebugRange = 2048f;

	public void Register( Node node, GameObject refObject, Component refComponent )
	{
		var key = (NodeKey)node;

		if ( !_sceneRefNodes.TryGetValue( key, out var sceneRef ) || !sceneRef.Node.TryGetTarget( out var target ) || target != node )
		{
			_sceneRefNodes[key] = sceneRef = new SceneRefNode( key, new WeakReference<Node>( node ) );
		}

		sceneRef.ReferencedObject = refObject;
		sceneRef.ReferencedComponent = refComponent;
	}

	public void Trigger( Node node )
	{
		var key = (NodeKey)node;

		if ( !_sceneRefNodes.TryGetValue( (NodeKey)node, out var sceneRef ) )
		{
			return;
		}

		sceneRef.LastTriggered = 0f;

		if ( _lastTriggered.Count == 0 || _lastTriggered[0] != key )
		{
			_lastTriggered.Remove( key );
			_lastTriggered.Insert( 0, key );
		}
	}

	[ThreadStatic] private static List<NodeKey> _toRemove;

	private void CleanUpSceneRefs()
	{
		_toRemove ??= new List<NodeKey>();
		_toRemove.Clear();

		foreach ( var pair in _sceneRefNodes )
		{
			if ( !pair.Value.IsValid )
			{
				_toRemove.Add( pair.Key );
			}
		}

		foreach ( var key in _toRemove )
		{
			_sceneRefNodes.Remove( key );
		}
	}

	private void CleanUpTriggered()
	{
		while ( _lastTriggered.Count > 0 )
		{
			var key = _lastTriggered[^1];

			if ( _sceneRefNodes.TryGetValue( key, out var sceneRef ) && sceneRef.LastTriggered < 1f )
			{
				break;
			}

			_lastTriggered.RemoveAt( _lastTriggered.Count - 1 );
		}
	}

	private static Color GetSceneReferenceGizmoColor( bool selected, Color baseColor, float time, float distanceAlpha )
	{
		var alpha = selected ? 1f : MathX.Clamp( 4f - time * 4f, 0f, 1f );
		var t = MathF.Pow( Math.Max( 1f - time, 0f ), 8f );

		return Color.Lerp( baseColor, Color.White, t )
			.WithAlpha( alpha * distanceAlpha );
	}

	private static float GetScreenEdgeFade( Vector2 screenPos, float margin )
	{
		var size = Gizmo.Camera.Size;

		var distance = Math.Min( Math.Min( screenPos.x, screenPos.y ),
			Math.Min( size.x - screenPos.x, size.y - screenPos.y ) );

		return (distance / margin).Clamp( 0f, 1f );
	}

	private bool IsActionGraphLinkInRange( GameObject referenced, out float distSqr )
	{
		if ( referenced == GameObject )
		{
			distSqr = 0f;
			return false;
		}

		var line = new Line( GameObject.WorldPosition, referenced.WorldPosition );
		distSqr = line.SqrDistance( Gizmo.CameraTransform.Position );

		return distSqr < MaxActionGraphLinkDebugRange * MaxActionGraphLinkDebugRange;
	}

	private static Color SceneRefDebugLineColor { get; } = Color.Parse( "#E6DB74" )!.Value;

	public void Draw()
	{
		CleanUpSceneRefs();
		CleanUpTriggered();

		var showAll = Gizmo.IsSelected; // || Gizmo.Settings.DebugActionGraphs;

		if ( !showAll && _lastTriggered.Count == 0 || _sceneRefNodes.Count == 0 )
		{
			return;
		}

		var keys = showAll ? _sceneRefNodes.Keys : _lastTriggered.AsEnumerable();
		var sceneRefs = keys
			.Select( x => _sceneRefNodes.GetValueOrDefault( x ) )
			.Where( x => x != null );

		foreach ( var group in sceneRefs.GroupBy( x => x.ReferencedObject ) )
		{
			if ( !IsActionGraphLinkInRange( group.Key, out var distSqr ) )
			{
				continue;
			}

			var alpha = MathX.Clamp( 16f - MathF.Sqrt( distSqr ) * 16f / MaxActionGraphLinkDebugRange, 0f, 1f );

			using var groupScope = Gizmo.Scope( group.Key.Name );

			Gizmo.Transform = global::Transform.Zero;

			Gizmo.Draw.IgnoreDepth = true;
			Gizmo.Hitbox.DepthBias = 0.1f;

			var minTime = group.Min( x => (float)x.LastTriggered );

			if ( Gizmo.Camera.ToScreen( new Line( GameObject.WorldPosition, group.Key.WorldPosition ) ) is not { } screenLine )
			{
				continue;
			}

			var screenRect = new Rect( 0f, Gizmo.Camera.Size );

			if ( screenLine.Clip( screenRect ) is null )
			{
				continue;
			}

			var refPos = group.Key.WorldPosition;
			var camPos = Gizmo.Camera.Position;
			var camForward = Gizmo.CameraTransform.Forward;
			var anyHovered = false;

			const float margin = 18f;
			const string fontFamily = "Roboto";
			const float fontSize = 14f;
			const int fontWeight = 500;
			const TextFlag textFlags = TextFlag.Center | TextFlag.SingleLine;

			var textFlipped = screenLine.End.x < screenLine.Start.x;
			var textAngle = textFlipped ? screenLine.AngleDegrees + 180f : screenLine.AngleDegrees;
			var textOffset = screenLine.Tangent.Perpendicular * (textFlipped ? margin : -margin);

			var relTextPos = textOffset;

			foreach ( var item in group.GroupBy( x => x.NodeKey.GraphId ).Select( x => x.MaxBy( y => y.LastTriggered.Absolute ) ) )
			{
				if ( !item.Node.TryGetTarget( out var node ) )
				{
					continue;
				}

				if ( (screenLine + relTextPos).Clip( screenRect ) is not { } textLine )
				{
					continue;
				}

				var text = node.ActionGraph.Title;
				var clip = new Vector2( textLine.Length - 8f, 1000f );

				var texture = TextRendering.GetOrCreateTexture( new TextRendering.Scope( text, Gizmo.Draw.Color, fontSize, fontFamily, fontWeight ), clip, textFlags );

				if ( texture.Width <= 16f )
				{
					continue;
				}

				using var itemScope = Gizmo.ObjectScope( node, global::Transform.Zero );

				var hovered = textLine
					.WithLength( texture.Width - margin )
					.Distance( Gizmo.Active.Input.CursorPosition ) < margin * 0.5f;

				Gizmo.Draw.Color = GetSceneReferenceGizmoColor( showAll, Color.White.Darken( hovered ? 0f : 0.2f ), item.LastTriggered, alpha );
				Gizmo.Draw.ScreenText( text, textLine.Center, clip, textAngle,
					font: fontFamily,
					flags: textFlags,
					size: fontSize );

				relTextPos += textOffset;

				if ( hovered && !anyHovered )
				{
					Gizmo.Active.builder.HoveredPath = Gizmo.Path;
					Gizmo.Active.builder.HitDistance = 0f;
				}

				if ( Gizmo.WasClicked && Gizmo.Settings.Selection )
				{
					// TODO: select all nodes in group
					Gizmo.Select();
				}

				anyHovered |= hovered;
			}

			var pulse = 1f + MathF.Pow( Math.Max( 1f - minTime, 0f ), 8f ) * 3f;
			var dist = (refPos - camPos).Dot( camForward );

			Gizmo.Draw.Color = GetSceneReferenceGizmoColor( showAll, SceneRefDebugLineColor.Desaturate( anyHovered ? 0.75f : 0f ), minTime, alpha );
			Gizmo.Draw.LineThickness = pulse;

			using ( Gizmo.Hitbox.LineScope() )
			{
				Gizmo.Draw.Line( GameObject.WorldPosition, refPos );
			}

			Gizmo.Draw.LineCircle( refPos, Gizmo.LocalCameraTransform.Rotation.Forward, pulse * dist / 32f );
		}
	}
}

