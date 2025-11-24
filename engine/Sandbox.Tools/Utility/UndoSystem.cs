using System;
using static Editor.Label;
using static Sandbox.Scene;

namespace Sandbox.Helpers;

/// <summary>
/// A system that aims to wrap the main reusable functionality of an undo system
/// </summary>
public partial class UndoSystem
{
	public class Entry
	{
		public string Name { get; set; }
		public Action Undo { get; set; }
		public Action Redo { get; set; }
		/// [Obsolete]?
		public Object Image { get; set; }
		public DateTime Timestamp { get; set; }
		public bool Locked { get; set; }
	}

	/// <summary>
	/// Called when an undo is run
	/// </summary>
	public Action<Entry> OnUndo;

	/// <summary>
	/// Called when a redo is run
	/// </summary>
	public Action<Entry> OnRedo;

	/// <summary>
	/// Backwards stack
	/// </summary>
	public Stack<Entry> Back { get; } = new();

	/// <summary>
	/// Forwards stack, gets cleared when a new undo is added
	/// </summary>
	public Stack<Entry> Forward { get; } = new();

	/// <summary>
	/// Instigate an undo. Return true if we found a successful undo
	/// </summary>
	public bool Undo()
	{
		if ( !Back.TryPop( out var entry ) )
		{
			next = initial;
			return false;
		}

		next = entry.Undo;
		try
		{
			entry.Undo?.Invoke();
		}
		catch ( System.Exception e )
		{
			Log.Warning( e, $"Error when undoing '{entry.Name}': {e.Message}" );
		}

		if ( entry.Locked )
		{
			Back.Push( entry );
			return false;
		}

		Forward.Push( entry );
		OnUndo?.Invoke( entry );

		return true;
	}

	/// <summary>
	/// Instigate a redo, returns true if we found a successful undo
	/// </summary>
	public bool Redo()
	{
		if ( !Forward.TryPop( out var entry ) )
			return false;

		next = entry.Redo;
		Back.Push( entry );
		entry.Redo?.Invoke();
		OnRedo?.Invoke( entry );

		return true;
	}

	/// <summary>
	/// Insert a new undo entry
	/// </summary>
	public Entry Insert( string title, Action undo, Action redo = null )
	{
		var e = new Entry
		{
			Name = title,
			Undo = undo,
			Redo = redo,
			Timestamp = DateTime.Now,
		};

		Back.Push( e );

		Forward.Clear();

		return e;
	}

	/// <summary>
	/// Provide a function that returns an action to call on undo/redo.
	/// This generally is a function that saves and restores the entire state
	/// of a project.
	/// </summary>
	[Obsolete( "Auto Snapshotting is obsolete and no longer working. If you really want to use snapshotting for Undo, create/restore the snapshots manually in the undo/redo actions provided to UndoSystem.Insert" )]
	public void SetSnapshotFunction( Func<Action> snapshot )
	{
	}

	/// <code>
	///  func getsnapshot()
	///  {
	///		var state = currentstate();
	///
	///		return () => restorestate( state );
	///  }
	///
	///  startup()
	///  {
	///     -- give a function that creates undo functions
	///     UndoSystem.SetSnapshotter( getsnapshot )
	///
	///     -- store current snapshot in `next`
	///     UndoSystem.Initialize();
	///  }
	///
	///  mainloop()
	///  {
	///     deleteobject();
	///
	///     -- store 'next' snapshot as "object deleted" undo
	///     -- take a new snapshot and store it in next
	///     UndoSystem.Snapshot( "object deleted" );
	///  }
	/// </code>
	Action next;
	Action initial;

	/// <summary>
	/// Should be called after you make a change to your project. The snapshot system
	/// is good for self contained projects that can be serialized and deserialized quickly.
	/// </summary>
	[Obsolete( "Auto Snapshotting is obsolete and no longer working. If you really want to use snapshotting for Undo, create/restore the snapshots manually in the undo/redo actions provided to UndoSystem.Insert" )]
	public void Snapshot( string changeTitle )
	{
	}

	/// <summary>
	/// Clear the history and take an initial snapshot.
	/// You should call this right after a load, or a new project.
	/// </summary>
	public void Initialize()
	{
		Back.Clear();
		Forward.Clear();

		initial = next;
	}
}
