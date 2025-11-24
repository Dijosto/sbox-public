using System;

namespace Editor;

/// <summary>
/// A control widget is used to edit the value of a single SerializedProperty.
/// </summary>
public abstract class ComponentEditorWidget : Widget
{
	public SerializedObject SerializedObject { get; private set; }

	public ComponentEditorWidget( SerializedObject obj ) : base( null )
	{
		ArgumentNullException.ThrowIfNull( obj, "SerializedObject" );

		SerializedObject = obj;
		SetSizeMode( SizeMode.Flexible, SizeMode.CanShrink );
	}

	public static ComponentEditorWidget Create( SerializedObject obj )
	{
		var editor = EditorTypeLibrary.GetTypesWithAttribute<CustomEditorAttribute>( false )
					.Where( x => x.Type.TargetType.IsAssignableTo( typeof( ComponentEditorWidget ) ) )
					.Where( x => x.Attribute.TargetType.Name == obj.TypeName )
					.FirstOrDefault();

		if ( editor.Type == null ) return null;
		return editor.Type.Create<ComponentEditorWidget>( new object[] { obj } );
	}

	public virtual void OnHeaderContextMenu( Menu menu )
	{

	}
}
