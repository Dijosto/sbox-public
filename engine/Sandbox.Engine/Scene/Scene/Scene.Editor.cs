namespace Sandbox;

public partial class Scene : GameObject
{
	/// <summary>
	/// Allows access to the scene's editor session from the game. This will be null if there is no
	/// editor session active on this scene.
	/// </summary>
	public ISceneEditorSession Editor { get; internal set; }

	public interface ISceneEditorSession
	{
		/// <summary>
		/// True if this scene has unsaved changes
		/// </summary>
		bool HasUnsavedChanges { get; set; }

		/// <summary>
		/// You have changed the editor's selection, add a new undo entry
		/// </summary>
		[Obsolete]
		void AddSelectionUndo();

		[Obsolete]
		void OnEditLog( string name, object source );

		/// <summary>
		/// Focus the editor camera onto this box
		/// </summary>
		void FrameTo( in BBox box );

		/// <summary>
		/// Save this scene to disk
		/// </summary>
		void Save( bool forceSaveAs );

		/// <summary>
		/// Tell undo about this property change
		/// </summary>
		[Obsolete]
		void RecordChange( SerializedProperty property );

		/// <summary>
		/// Add a new undo entry
		/// </summary>
		void AddUndo( string name, Action undo, Action redo );

		ISceneUndoScope UndoScope( string name );

		/// <summary>
		/// If we have any gameobjects selected, return the first one
		/// </summary>
		public GameObject SelectedGameObject => GetSelection().OfType<GameObject>().FirstOrDefault();

		/// <summary>
		/// Gets the current selection from the editor
		/// </summary>
		public IEnumerable<object> GetSelection();
	}
}

[Flags]
public enum GameObjectUndoFlags
{
	Properties = 0,
	Components = 1,
	Children = 2,
	All = Properties | Components | Children
}

public interface ISceneUndoScope
{
	ISceneUndoScope WithGameObjectCreations();
	ISceneUndoScope WithGameObjectDestructions( IEnumerable<GameObject> gameObjects );
	ISceneUndoScope WithGameObjectDestructions( GameObject gameObject );
	ISceneUndoScope WithGameObjectChanges( IEnumerable<GameObject> objects, GameObjectUndoFlags flags );
	ISceneUndoScope WithGameObjectChanges( GameObject gameObject, GameObjectUndoFlags flags );
	ISceneUndoScope WithComponentCreations();
	ISceneUndoScope WithComponentDestructions( IEnumerable<Component> components );
	ISceneUndoScope WithComponentDestructions( Component component );
	ISceneUndoScope WithComponentChanges( IEnumerable<Component> components );
	ISceneUndoScope WithComponentChanges( Component component );
	IDisposable Push();
}
