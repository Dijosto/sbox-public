using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sandbox;

/// <summary>
/// Renders text in the world
/// </summary>
[Expose]
[Title( "Text Renderer" )]
[Category( "Rendering" )]
[Icon( "font_download" )]
[EditorHandle( "materials/gizmo/text_renderer.png" )]
public sealed class TextRenderer : Renderer, Component.ExecuteInEditor
{
	SceneObject _sceneObject;

	/// <summary>
	/// Represents the horizontal alignment of the text.
	/// </summary>
	public enum HAlignment
	{
		[Icon( "align_horizontal_left" )]
		Left = 1,

		[Icon( "align_horizontal_center" )]
		Center = 2,

		[Icon( "align_horizontal_right" )]
		Right = 3,
	}

	/// <summary>
	/// Represents the vertical alignment of the text.
	/// </summary>
	public enum VAlignment
	{
		[Icon( "align_vertical_top" )]
		Top = 1,

		[Icon( "align_vertical_center" )]
		Center = 2,

		[Icon( "align_vertical_bottom" )]
		Bottom = 3,
	}

	/// <summary>
	/// The text scope defines what text to render and it's visual properties (such as font, color, outline, etc.)
	/// </summary>
	[Property]
	public TextRendering.Scope TextScope
	{
		get => _textScope;
		set
		{
			_textScope = value;
			OnPropertyDirty();
		}
	}
	TextRendering.Scope _textScope = new TextRendering.Scope( "Hello! ❤", Color.White, 32.0f, "Poppins", 400 );

	/// <summary>
	/// The size of the text in the world. This is different from the font size, which is defined in the TextScope and determines resolution of the rendered text.
	/// </summary>
	[Property, Range( 0, 2 )]
	public float Scale
	{
		get => _scale;
		set
		{
			if ( _scale == value ) return;
			_scale = value;
			OnPropertyDirty();
		}
	}
	float _scale = 1.0f;

	/// <summary>
	/// The horizontal alignment of the text in the world.
	/// </summary>
	[Property]
	public HAlignment HorizontalAlignment
	{
		get => _horizontalAlignment;
		set
		{
			if ( _horizontalAlignment == value ) return;
			_horizontalAlignment = value;
			OnPropertyDirty();
		}
	}
	HAlignment _horizontalAlignment = HAlignment.Center;

	/// <summary>
	/// The vertical alignment of the text in the world.
	/// </summary>
	[Property]
	public VAlignment VerticalAlignment
	{
		get => _verticalAlignment;
		set
		{
			if ( _verticalAlignment == value ) return;
			_verticalAlignment = value;
			OnPropertyDirty();
		}
	}
	VAlignment _verticalAlignment = VAlignment.Center;

	/// <summary>
	/// The blend mode of the text. This determines how the text is rendered over the world.
	/// </summary>
	[Property]
	public BlendMode BlendMode
	{
		get => _blendMode;
		set
		{
			if ( _blendMode == value ) return;
			_blendMode = value;
			OnPropertyDirty();
		}
	}
	BlendMode _blendMode = BlendMode.Normal;

	/// <summary>
	/// The strength of the fog effect applied to the text. This determines how much the text blends with any fog in the scene.
	/// </summary>
	[Property, Range( 0, 1 )] public float FogStrength { get; set; } = 1.0f;

	protected override void OnEnabled()
	{
		var so = new TextSceneObject( Scene.SceneWorld );
		so.Transform = WorldTransform.WithScale( WorldScale * Scale );
		so.Tags.SetFrom( GameObject.Tags );
		_sceneObject = so;
		OnSceneObjectCreated( _sceneObject );

		OnDirty();

		Transform.OnTransformChanged += TransformChanged;
	}

	protected override void OnDisabled()
	{
		Transform.OnTransformChanged -= TransformChanged;

		BackupRenderAttributes( _sceneObject?.Attributes );
		_sceneObject?.Delete();
		_sceneObject = null;
	}

	protected override void OnRenderOptionsChanged()
	{
		if ( _sceneObject.IsValid() )
		{
			RenderOptions.Apply( _sceneObject );
		}
	}

	protected override void OnDirty()
	{
		if ( _sceneObject is not TextSceneObject so )
			return;

		var transform = WorldTransform;
		so.Transform = transform.WithScale( transform.Scale * Scale );
		so.BlendMode = BlendMode;
		so.FogStrength = FogStrength;
		so.TextScope = TextScope;

		var vCenter = VerticalAlignment switch
		{
			VAlignment.Top => TextFlag.Top,
			VAlignment.Bottom => TextFlag.Bottom,
			_ => TextFlag.CenterVertically,
		};
		so.TextFlags = HorizontalAlignment switch
		{
			HAlignment.Left => TextFlag.Left | vCenter | TextFlag.DontClip,
			HAlignment.Center => TextFlag.CenterHorizontally | vCenter | TextFlag.DontClip,
			HAlignment.Right => TextFlag.Right | vCenter | TextFlag.DontClip,
			_ => TextFlag.CenterHorizontally | vCenter | TextFlag.DontClip,
		};

		RenderOptions.Apply( so );

		so.CalculateBounds();
	}

	void TransformChanged()
	{
		if ( _sceneObject is not TextSceneObject so )
			return;

		so.Transform = WorldTransform.WithScale( WorldScale * Scale );
		so.CalculateBounds();

	}

	/// <summary>
	/// Tags have been updated - lets update our scene object tags
	/// </summary>
	protected override void OnTagsChanged()
	{
		if ( !_sceneObject.IsValid() ) return;

		_sceneObject.Tags.SetFrom( GameObject.Tags );
	}

	/// <summary>
	/// The color of the text from the TextScope.
	/// </summary>
	public Color Color
	{
		get => _textScope.TextColor;
		set
		{
			_textScope.TextColor = value;

			if ( _sceneObject is TextSceneObject so )
				so.TextScope = _textScope;
		}
	}

	/// <summary>
	/// The font size of the text from the TextScope. This is different from the Scale, which determines how large the text appears in the world.
	/// </summary>
	public float FontSize
	{
		get => _textScope.FontSize;
		set
		{
			_textScope.FontSize = value;

			if ( _sceneObject is TextSceneObject so )
				so.TextScope = _textScope;
		}
	}
	public int FontWeight
	{
		get => _textScope.FontWeight;
		set
		{
			_textScope.FontWeight = value;

			if ( _sceneObject is TextSceneObject so )
				so.TextScope = _textScope;
		}
	}

	public string FontFamily
	{
		get => _textScope.FontName;
		set
		{
			_textScope.FontName = value;

			if ( _sceneObject is TextSceneObject so )
				so.TextScope = _textScope;
		}
	}

	public string Text
	{
		get => _textScope.Text;
		set
		{
			_textScope.Text = value;

			if ( _sceneObject is TextSceneObject so )
				so.TextScope = _textScope;
		}
	}

	public override int ComponentVersion => 2;

	[Expose, JsonUpgrader( typeof( TextRenderer ), 1 )]
	static void Upgrader_v1( JsonObject obj )
	{
		// shouldn't be nessecary
		if ( obj.ContainsKey( "TextScope" ) )
		{
			Log.Info( "Skipping - has TextScope" );
			return;
		}

		var ts = new TextRendering.Scope( "Hello! ❤", Color.White, 32.0f, "Poppins", 800 );

		ts.TextColor = obj.GetPropertyValue( "Color", ts.TextColor );
		ts.FontSize = obj.GetPropertyValue( "FontSize", ts.FontSize );
		ts.FontWeight = obj.GetPropertyValue( "FontWeight", ts.FontWeight );
		ts.FontName = obj.GetPropertyValue( "FontFamily", ts.FontName );
		ts.Text = obj.GetPropertyValue( "Text", ts.Text );

		obj["TextScope"] = JsonSerializer.SerializeToNode( ts );
	}

	[Expose, JsonUpgrader( typeof( TextRenderer ), 2 )]
	static void Upgrader_v2( JsonObject obj )
	{
		if ( obj["TextScope"] is JsonObject scope && !scope.ContainsKey( "FilterMode" ) )
		{
			scope["FilterMode"] = "Bilinear";
		}
	}
}

file class TextSceneObject : SceneCustomObject
{
	public TextFlag TextFlags { get; set; } = TextFlag.DontClip | TextFlag.Center;
	public BlendMode BlendMode { get; set; } = BlendMode.Normal;
	public float FogStrength { get; set; } = 1.0f;

	private TextRendering.Scope _textScope;
	public TextRendering.Scope TextScope
	{
		get => _textScope;
		set
		{
			_textScope = value;

			var text = _textScope.Text;

			if ( !string.IsNullOrWhiteSpace( text ) && text.Length > 1 && text[0] == '#' )
			{
				var token = text[1..];
				text = Game.Language.GetPhrase( token );

				if ( text != token )
				{
					_textScope.Text = text;
				}
			}
		}
	}

	public TextSceneObject( SceneWorld world ) : base( world )
	{
		RenderLayer = SceneRenderLayer.Default;
	}

	public override void RenderSceneObject()
	{
		if ( string.IsNullOrWhiteSpace( TextScope.Text ) )
			return;

		Graphics.Attributes.SetCombo( "D_WORLDPANEL", 1 );
		Graphics.Attributes.SetComboEnum( "D_BLENDMODE", BlendMode );
		Graphics.Attributes.Set( "g_FogStrength", FogStrength );

		// Set a dummy WorldMat matrix so that ScenePanelObject doesn't break the transforms.
		Matrix mat = Matrix.CreateRotation( Rotation.From( 0, -90, 90 ) );
		Graphics.Attributes.Set( "WorldMat", mat );

		Graphics.DrawText( new Rect( 0 ), TextScope, TextFlags );
	}

	public void CalculateBounds()
	{
		if ( string.IsNullOrWhiteSpace( TextScope.Text ) )
		{
			LocalBounds = BBox.FromPositionAndSize( 0, 1 );
			return;
		}

		var tx = Transform;
		var scale = tx.Scale;
		var x = Graphics.MeasureText( new Rect( 0 ), TextScope, TextFlags );
		var center = new Vector3( 0.0f,
			TextFlags.Contains( TextFlag.Right ) ? x.Width * 0.5f :
			TextFlags.Contains( TextFlag.Left ) ? -x.Width * 0.5f : 0.0f,
			TextFlags.Contains( TextFlag.Bottom ) ? x.Height * 0.5f :
			TextFlags.Contains( TextFlag.Top ) ? -x.Height * 0.5f : 0.0f );

		var bounds = BBox.FromPositionAndSize( center * scale, new Vector3( 2, x.Width * scale.y, x.Height * scale.z ) );
		Bounds = bounds.Transform( tx.WithScale( 1 ) );
	}
}
