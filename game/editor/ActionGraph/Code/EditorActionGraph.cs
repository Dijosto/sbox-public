using Editor.NodeEditor;
using Facepunch.ActionGraphs;
using Sandbox;
using Sandbox.ActionGraphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Editor.ActionGraphs;

public partial class EditorActionGraph : IGraph
{
	[Hide]
	public ActionGraph Graph { get; }

	[Hide]
	private Dictionary<Node, EditorNode> NodeDict { get; } = new();

	[Hide]
	private HashSet<EditorNode> DirtyNodes { get; } = new();

	[Hide]
	public IEnumerable<INode> Nodes => NodeDict.Values;

	[Hide]
	public object HostObject { get; }

	[Hide]
	public event Action GraphPropertiesChanged;

	public string Title
	{
		get => Graph.Title;
		set => Graph.Title = value;
	}

	public string Description
	{
		get => Graph.Description;
		set => Graph.Description = value;
	}

	public string Category
	{
		get => Graph.Category;
		set => Graph.Category = value;
	}

	[IconName]
	public string Icon
	{
		get => Graph.Icon;
		set => Graph.Icon = value;
	}

	[Hide]
	public bool CanModifyParameters { get; private set; }

	internal static void SetTarget( ActionGraph graph, object value )
	{
		// Hacky fix for graphs targeting the root of prefabs
		var type = value is PrefabScene ? typeof( GameObject ) : value.GetType();

		graph.SetParameters( graph.Inputs.Values.With( InputDefinition.Target( type, defaultValue: value ) ), graph.Outputs.Values.ToArray() );
	}

	internal static void SetTargetType( ActionGraph graph, Type type )
	{
		graph.SetParameters( graph.Inputs.Values.With( InputDefinition.Target( type ) ), graph.Outputs.Values.ToArray() );
	}

	/// <summary>
	/// Target type of the object this graph runs on.
	/// </summary>
	[Hide]
	public Type TargetType => Graph.Inputs.Values.FirstOrDefault( x => x.IsTarget )?.Type;

	[Hide]
	public IEnumerable<Type> AvailableComponentTypes => (HostObject as IComponentLister)?
		.GetAll<Component>( FindMode.EverythingInSelf )
		.Select( x => x.GetType() )
		?? (TargetType is { } type && type.IsAssignableTo( typeof( Component ) )
			? new[] { type }
			: Enumerable.Empty<Type>());

	[Hide]
	public Scene HostScene => HostObject switch
	{
		GameObject go => go.Scene,
		Component comp => comp.Scene,
		_ => null
	};

	public void AddNode( INode node )
	{
		if ( node is not EditorNode actionNode )
		{
			return;
		}

		NodeDict[actionNode.Node] = actionNode;
	}

	public void RemoveNode( INode node )
	{
		if ( node is not EditorNode actionNode )
		{
			return;
		}

		NodeDict.Remove( actionNode.Node );

		if ( !actionNode.Node.IsValid )
		{
			return;
		}

		actionNode.Node.Remove();

		var referencedVars = actionNode.Node.Properties.Values
			.Select( x => x.Value )
			.OfType<Variable>()
			.ToArray();

		foreach ( var variable in referencedVars )
		{
			if ( variable.IsValid && !variable.References.Any() )
			{
				Log.Info( $"No more references to {variable}" );
				Graph.RemoveVariable( variable );
			}
		}
	}

	internal void MarkDirty( IEnumerable<Node> nodes )
	{
		foreach ( var node in nodes )
		{
			MarkDirty( node );
		}
	}

	internal void MarkDirty( Node node )
	{
		FindNode( node )?.MarkDirty();
	}

	internal void MarkDirty( EditorNode node )
	{
		DirtyNodes.Add( node );

		if ( node.Node.Definition == EditorNodeLibrary.Input )
		{
			GraphPropertiesChanged?.Invoke();
		}
	}

	public EditorNode FindNode( Node node )
	{
		if ( node == null ) return null;

		while ( node.Parent is { } parent )
		{
			node = parent;
		}

		return NodeDict.TryGetValue( node, out var editorNode ) ? editorNode : null;
	}

	private record LabeledOutputLinkModel( string Label, string DstName, int? DstIndex = null );

	private const string LabelLinksUserDataKey = "LabelLinks";

	private IDisposable PushContext()
	{
		return HostScene?.Push();
	}

	public string SerializeNodes( IEnumerable<INode> nodes )
	{
		using ( PushContext() )
		{
			var sourceNodes = nodes.OfType<EditorNode>()
				.Select( x => x.Node )
				.ToArray();

			foreach ( var sourceNode in sourceNodes )
			{
				var labelLinks = sourceNode.Links
					.Where( x => x.Source.Node.Parent != x.Target.Node )
					.Where( x => !sourceNodes.Contains( x.Source.Node ) )
					.Where( x => x.Source.GetLabel() is not null )
					.Select( x => new LabeledOutputLinkModel(
						x.Source.GetLabel(),
						x.Target.Name,
						x.IsArrayElement ? x.ArrayIndex : null ) )
					.ToArray();

				if ( labelLinks.Any() )
				{
					sourceNode.UserData[LabelLinksUserDataKey] = Json.ToNode( labelLinks );
				}
			}

			try
			{
				return Graph.Serialize( sourceNodes, EditorJsonOptions );
			}
			finally
			{
				foreach ( var sourceNode in sourceNodes )
				{
					sourceNode.UserData.Remove( LabelLinksUserDataKey );
				}
			}
		}
	}

	public IEnumerable<INode> DeserializeNodes( string serialized )
	{
		using ( PushContext() )
		{
			var labeledOutputDict = Graph.Nodes.Values.SelectMany( x => x.Outputs.Values )
				.Select( x => (Label: x.GetLabel(), Output: x) )
				.Where( x => x.Label is not null )
				.DistinctBy( x => x.Label )
				.ToDictionary( x => x.Label, x => x.Output );

			var result = Graph.DeserializeInsert( serialized, EditorJsonOptions );

			foreach ( var node in result.Nodes )
			{
				if ( !node.UserData.TryGetPropertyValue( LabelLinksUserDataKey, out var labelLinksNode ) )
				{
					continue;
				}

				node.UserData.Remove( LabelLinksUserDataKey );

				if ( labelLinksNode?.Deserialize<LabeledOutputLinkModel[]>() is not { } labelLinks )
				{
					continue;
				}

				foreach ( var labelLink in labelLinks )
				{
					if ( !labeledOutputDict.TryGetValue( labelLink.Label, out var output ) )
					{
						continue;
					}

					if ( !node.Inputs.TryGetValue( labelLink.DstName, out var input ) )
					{
						continue;
					}

					if ( labelLink.DstIndex is { } index )
					{
						if ( input.LinkArray is { Count: var count } && count > index )
						{
							input.SetLink( output, index );
						}
						else
						{
							input.InsertLink( output, index );
						}
					}
					else
					{
						input.SetLink( output );
					}
				}
			}

			UpdateNodes();

			return result.Nodes
				.Select( x => NodeDict.TryGetValue( x, out var actionNode ) ? actionNode : null )
				.Where( x => x != null );
		}
	}

	private IDisposable PushTarget()
	{
		return ActionGraph.PushTarget( Graph.Inputs.Values.FirstOrDefault( x => x.IsTarget ) );
	}

	public string Serialize()
	{
		using var sceneScope = PushContext();
		using var targetScope = PushTarget();

		return JsonSerializer.Serialize( Graph, EditorJsonOptions );
	}

	public void Deserialize( string serialized )
	{
		using var sceneScope = PushContext();
		using var targetScope = PushTarget();

		Graph.Deserialize( serialized, null, EditorJsonOptions );

		UpdateNodes();
	}

	public EditorActionGraph( Facepunch.ActionGraphs.ActionGraph graph )
	{
		Graph = graph;
		HostObject = graph.GetEmbeddedTarget();

		UpdateNodes();
		UpdateEditorProperties();
	}

	private void UpdateEditorProperties()
	{
		var propertiesEvent = new GetEditorPropertiesEvent( this )
		{
			CanModifyParameters = Graph.SourceLocation is GameResourceSourceLocation { Resource: ActionGraphResource }
		};

		EditorEvent.Run( GetEditorPropertiesEvent.EventName, propertiesEvent );

		CanModifyParameters = propertiesEvent.CanModifyParameters;
	}

	/// <summary>
	/// Keep <see cref="NodeDict"/> in sync with <see cref="Graph"/>.
	/// </summary>
	internal void UpdateNodes()
	{
		if ( Graph.PrimaryOutputNode is { } primaryOutput && primaryOutput.UserData["Position"] is null )
		{
			primaryOutput.UserData["Position"] = Json.ToNode( new Vector2( 256f, 0f ) );
		}

		foreach ( var node in Graph.Nodes.Values )
		{
			if ( node.Parent is not null )
			{
				continue;
			}

			if ( !NodeDict.TryGetValue( node, out var editorNode ) )
			{
				editorNode = LibraryNodeType.CreateEditorNode( this, node );
				NodeDict.Add( node, editorNode );
			}
			else
			{
				editorNode.InvalidateUserData();
				editorNode.MarkDirty();
			}
		}

		foreach ( var (node, editorNode) in NodeDict.Where( x => !x.Key.IsValid ).ToArray() )
		{
			NodeDict.Remove( node );
		}
	}

	[Hide]
	private ValidationMessage[] _prevMessages = Array.Empty<ValidationMessage>();

	private static bool HaveMessagesChanged(
		IReadOnlyCollection<ValidationMessage> oldMessages,
		IReadOnlyCollection<ValidationMessage> newMessages )
	{
		if ( oldMessages.Count != newMessages.Count )
		{
			return true;
		}

		foreach ( var (oldMessage, newMessage) in oldMessages.Zip( newMessages ) )
		{
			if ( !oldMessage.Equals( newMessage ) )
			{
				return true;
			}
		}

		return false;
	}

	private static Node GetNodeFromContext( IMessageContext ctx )
	{
		while ( true )
		{
			if ( ctx == null )
			{
				return null;
			}

			if ( ctx is Node node )
			{
				return node;
			}

			ctx = ctx.Parent;
		}
	}

	public IReadOnlyList<INode> Update()
	{
		var messages = Graph.Messages;

		if ( HaveMessagesChanged( _prevMessages, messages ) )
		{
			foreach ( var message in _prevMessages )
			{
				if ( GetNodeFromContext( message.Context ) is { } node && FindNode( node ) is { } actionNode )
				{
					MarkDirty( actionNode );
				}
			}

			_prevMessages = messages.ToArray();

			foreach ( var message in _prevMessages )
			{
				if ( GetNodeFromContext( message.Context ) is { } node && FindNode( node ) is { } actionNode )
				{
					MarkDirty( actionNode );
				}
			}
		}

		if ( DirtyNodes.Count == 0 )
		{
			return Array.Empty<INode>();
		}

		var dirty = DirtyNodes.ToArray();

		foreach ( var actionNode in dirty )
		{
			actionNode.Update();
		}

		DirtyNodes.Clear();

		return dirty;
	}
}
