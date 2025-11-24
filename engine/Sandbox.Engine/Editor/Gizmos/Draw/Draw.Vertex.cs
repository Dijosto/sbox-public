using Microsoft.Win32;
using System.Collections.Immutable;

namespace Sandbox;

public static partial class Gizmo
{
	public sealed partial class GizmoDraw
	{
		ScopeState _lastState;


		private bool CanReuseVertexObject( Graphics.PrimitiveType type, Material material )
		{
			if ( !_vertexObject.IsValid() ) return false;
			if ( _vertexObject.managedNative.Material != material.native ) return false;
			if ( _vertexObject.PrimitiveType != type ) return false;
			if ( _vertexObjectPath != Path ) return false;

			//if ( type == Graphics.PrimitiveType.Lines )
			{
				if ( _lastState.LineThickness != LineThickness ) return false;
				if ( _lastState.IgnoreDepth != IgnoreDepth ) return false;
			}


			return true;
		}

		VertexSceneObject VertexObject( Graphics.PrimitiveType type, Material material, bool tryAdd = true )
		{
			// Write the vertex buffer of the previous queued object when we start a new one
			_vertexObject?.Write();

			if ( CanReuseVertexObject( type, material ) && tryAdd )
			{
				return _vertexObject;
			}

			_vertexObjectPath = Path;

			_vertexObject = Active.FindOrCreate<VertexSceneObject>( $"line", () => new VertexSceneObject( World ) );
			_vertexObject.PrimitiveType = type;
			_vertexObject.Material = material;
			_vertexObject.Transform = Transform;
			_vertexObject.Vertices.Clear();
			_vertexObject.ColorTint = Color;
			_vertexObject.Bounds = BBox.FromPositionAndSize( 0, float.MaxValue );
			_vertexObject.RenderLayer = IgnoreDepth ? SceneRenderLayer.OverlayWithoutDepth : SceneRenderLayer.OverlayWithDepth;

			// todo - tell VO to determine flags from material
			if ( !IgnoreDepth )
			{
				_vertexObject.Flags.IsTranslucent = false;
				_vertexObject.Flags.IsOpaque = true;
			}

			//
			// This is all just taped together right now. 
			//
			// https://github.com/orgs/Facepunch/projects/17/views/1?pane=issue&itemId=22115064
			//

			if ( type == Graphics.PrimitiveType.Lines )
			{
				_vertexObject.Attributes.Set( "LineThickness", LineThickness );
				//_vertexObject.Attributes.Set( "PatternType", LineSettings.Dashed ? 1.0f : 0.0f );
			}

			_vertexObject.Attributes.SetCombo( "D_NO_ZTEST", IgnoreDepth ? 1 : 0 );
			_vertexObject.Attributes.SetCombo( "D_NO_CULLING", CullBackfaces ? 0 : 1 );
			_vertexObject.Attributes.SetCombo( "D_SNAP_TO_SCREEN_PIXELS", 0 );
			_vertexObject.Attributes.SetCombo( "D_SHADED", 0 );
			_vertexObject.Attributes.SetCombo( "D_DEPTH_BIAS", 1 );

			_lastState = Active.scope;

			return _vertexObject;
		}

	}



}
