using Facepunch.ActionGraphs;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

#nullable enable

namespace Sandbox.ActionGraphs;

public static class ActionGraphDebugger
{
	private class RegisteredActionGraph
	{
		public List<WeakReference<ActionGraph>> Instances { get; } = new();
		public Expression? LastCompiled { get; set; }
	}

	private static NodeLibrary? _nodeLibrary;

	private static List<WeakReference<ActionGraph>> Unregistered { get; } = new();
	private static Dictionary<Guid, RegisteredActionGraph> Registered { get; } = new();
	private static Dictionary<Guid, LinkDebugger> ListeningLinks { get; } = new();

	private static void OnActionGraphCreated( ActionGraph graph )
	{
		lock ( Registered )
		{
			Unregistered.Add( new WeakReference<ActionGraph>( graph ) );
		}
	}

	private static void OnActionGraphCompiled( ActionGraph graph, Expression expression )
	{
		lock ( Registered )
		{
			if ( Registered.TryGetValue( graph.Guid, out var registered ) )
			{
				registered.LastCompiled = expression;
			}
		}
	}

	private static bool _enabled;

	public static bool Enabled
	{
		get => _enabled;
		set
		{
			if ( _enabled == value ) return;

			_enabled = value;

			if ( _enabled )
			{
				Initialize( Game.NodeLibrary );
			}
		}
	}

	internal static IReadOnlyList<ActionGraph> GetAllGraphs()
	{
		Tick();

		return Registered.Values
			.Select( x => x
				.Instances
				.Select( y => y.TryGetTarget( out var graph ) ? graph : null )
				.FirstOrDefault( y => y != null ) )
			.Where( x => x != null )
			.ToArray()!;
	}

	internal static void Tick()
	{
		Initialize( Enabled ? Game.NodeLibrary : null );

		if ( Enabled )
		{
			CheckUnregistered();
		}
	}

	public static bool TryGetGraph( Guid guid, [NotNullWhen( true )] out ActionGraph? graph )
	{
		Tick();

		lock ( Registered )
		{
			if ( !Registered.TryGetValue( guid, out var registered ) )
			{
				graph = null;
				return false;
			}

			graph = registered.Instances
				.Select( x => x.TryGetTarget( out var g ) ? g : null )
				.FirstOrDefault( x => x != null );

			return graph != null;
		}
	}

	public static bool TryGetCompiled( Guid guid, [NotNullWhen( true )] out Expression? expression )
	{
		lock ( Registered )
		{
			if ( !Registered.TryGetValue( guid, out var registered ) )
			{
				expression = null;
				return false;
			}

			expression = registered.LastCompiled;
			return expression is not null;
		}
	}

	private static List<Guid> ToRemove { get; } = new();
	private static RealTimeSince LastTick { get; set; }

	private static void CheckUnregistered()
	{
		var fullTick = LastTick > 1f;
		LastTick = 0f;

		lock ( Registered )
		{
			for ( var i = Unregistered.Count - 1; i >= 0; --i )
			{
				var weakRef = Unregistered[i];

				if ( !weakRef.TryGetTarget( out var graph ) )
				{
					Unregistered.RemoveAt( i );
					continue;
				}

				var guid = graph.Guid;

				Unregistered.RemoveAt( i );

				if ( !Registered.TryGetValue( guid, out var registered ) )
				{
					registered = new RegisteredActionGraph();
					Registered.Add( guid, registered );
				}

				registered.Instances.Add( weakRef );

				if ( ListeningLinks.TryGetValue( guid, out var linkDebugger ) )
				{
					graph.LinkTriggered += linkDebugger.LinkTriggered;
				}
			}

			if ( !fullTick ) return;

			ToRemove.Clear();

			foreach ( var (guid, registered) in Registered )
			{
				registered.Instances.RemoveAll( x => !x.TryGetTarget( out _ ) );

				if ( registered.Instances.Count == 0 )
				{
					Log.Info( $"No more references to {guid}" );
					ToRemove.Add( guid );
				}
			}

			foreach ( var guid in ToRemove )
			{
				Registered.Remove( guid );
			}
		}
	}

	private static void Initialize( NodeLibrary? nodeLibrary )
	{
		if ( _nodeLibrary == nodeLibrary )
		{
			return;
		}

		lock ( Registered )
		{
			if ( _nodeLibrary != null )
			{
				_nodeLibrary.ActionGraphCreated -= OnActionGraphCreated;
				_nodeLibrary.ActionGraphCompiled -= OnActionGraphCompiled;
			}

			Unregistered.Clear();
			Registered.Clear();
			ListeningLinks.Clear();

			_nodeLibrary = nodeLibrary;

			if ( _nodeLibrary != null )
			{
				_nodeLibrary.ActionGraphCreated += OnActionGraphCreated;
				_nodeLibrary.ActionGraphCompiled += OnActionGraphCompiled;
			}
		}
	}

	private static void AssertEnabled()
	{
		if ( !Enabled )
		{
			throw new InvalidOperationException( $"{nameof( ActionGraphDebugger )}.{nameof( Enabled )} is false!" );
		}
	}

	public static LinkDebugger StartListening( ActionGraph graph )
	{
		AssertEnabled();

		if ( graph.NodeLibrary != _nodeLibrary )
		{
			Log.Warning( $"Can't listen to graphs from a different NodeLibrary instance." );
			return new LinkDebugger( graph );
		}

		var guid = graph.Guid;

		if ( ListeningLinks.ContainsKey( guid ) )
		{
			throw new Exception( $"Already listening (Guid: {guid})." );
		}

		CheckUnregistered();

		lock ( Registered )
		{
			var listener = new LinkDebugger( graph );
			ListeningLinks.Add( guid, listener );

			if ( Registered.TryGetValue( guid, out var match ) )
			{
				foreach ( var weakRef in match.Instances )
				{
					if ( weakRef.TryGetTarget( out var registered ) )
					{
						registered.LinkTriggered += listener.LinkTriggered;
					}
				}
			}

			return listener;
		}
	}

	internal static void RemoveListener( LinkDebugger listener )
	{
		if ( listener.Graph.NodeLibrary != _nodeLibrary )
		{
			Log.Warning( $"Can't remove listener for graphs from a different NodeLibrary instance." );
			return;
		}

		lock ( Registered )
		{
			if ( !ListeningLinks.Remove( listener.GraphGuid, out var handler ) )
			{
				return;
			}

			if ( !Registered.TryGetValue( listener.GraphGuid, out var match ) )
			{
				return;
			}

			foreach ( var weakRef in match.Instances )
			{
				if ( weakRef.TryGetTarget( out var registered ) )
				{
					registered.LinkTriggered -= listener.LinkTriggered;
				}
			}
		}
	}
}

public sealed class LinkDebugger : IDisposable
{
	internal ActionGraph Graph { get; }
	internal Guid GraphGuid { get; }

	public event LinkTriggeredHandler? Triggered;

	internal LinkDebugger( ActionGraph graph )
	{
		Graph = graph;
		GraphGuid = graph.Guid;
	}

	internal void LinkTriggered( Link link, object? value )
	{
		if ( link.ActionGraph.NodeLibrary != Graph.NodeLibrary )
		{
			return;
		}

		var target = link.Target;

		if ( Graph == link.ActionGraph )
		{
			Triggered?.Invoke( link, value );
		}

		if ( !Graph.Nodes.TryGetValue( target.Node.Id, out var targetNode ) )
		{
			return;
		}

		if ( !targetNode.Inputs.TryGetValue( target.Name, out var matchingTarget ) )
		{
			return;
		}

		if ( !link.IsArrayElement )
		{
			if ( matchingTarget.Link is { } singleLink )
			{
				Triggered?.Invoke( singleLink, value );
			}
		}
		else if ( matchingTarget.LinkArray is { } linkArray && linkArray.Count > link.ArrayIndex )
		{
			Triggered?.Invoke( linkArray[link.ArrayIndex], value );
		}
	}

	public void Dispose()
	{
		ActionGraphDebugger.RemoveListener( this );
	}
}
