using Facepunch.ActionGraphs;
using Facepunch.ActionGraphs.Compilation;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace Sandbox.ActionGraphs;

[NodeDefinition]
internal partial class SceneRefNodeDefinition : NodeDefinition
{
	public const string Ident = "scene.ref";

	private PropertyDefinition ComponentProperty { get; } = new( "component", typeof( ComponentReference ),
		PropertyFlags.Required, new Facepunch.ActionGraphs.DisplayInfo( "Component", "Component this node will output.", Hidden: true ) );

	private PropertyDefinition GameObjectProperty { get; } = new( "gameobject", typeof( GameObjectReference ),
		PropertyFlags.Required, new Facepunch.ActionGraphs.DisplayInfo( "Game Object", "Game Object this node will output.", Hidden: true ) );

	private OutputDefinition Output { get; } = new( ParameterNames.Result, typeof( Either<GameObject, Component> ),
		0, new Facepunch.ActionGraphs.DisplayInfo( "Result", "The referenced value." ) );

	private NodeBinding DefaultBinding { get; }

	private NodeBinding ComponentBinding { get; }
	private NodeBinding GameObjectBinding { get; }

	public override Facepunch.ActionGraphs.DisplayInfo DisplayInfo { get; }

	#region Debug

	private static MethodInfo DispatchTriggered_Method { get; } =
		typeof( SceneRefGizmo ).GetMethod( nameof( SceneRefGizmo.Trigger ),
			BindingFlags.Public | BindingFlags.Instance )!;

	private Expression? BuildDispatchTriggeredExpression( Node node )
	{
		if ( node.ActionGraph.GetEmbeddedTarget() is not GameObject source )
		{
			return null;
		}

		if ( source.SceneRefGizmo is not { } gizmo )
		{
			return null;
		}

		return Expression.Call( Expression.Constant( gizmo ),
			DispatchTriggered_Method,
			Expression.Constant( node ) );
	}

	#endregion

	public SceneRefNodeDefinition( NodeLibrary nodeLibrary )
		: base( nodeLibrary, Ident )
	{
		DisplayInfo = new Facepunch.ActionGraphs.DisplayInfo( "Scene Reference",
			Description: "References a GameObject or Component from the scene this graph is embedded in.",
			Group: "Scene",
			Icon: "location_searching",
			Hidden: true );

		DefaultBinding = NodeBinding.Create( DisplayInfo,
			properties: new[] { ComponentProperty, GameObjectProperty, LegacyTypeProperty, LegacyValueProperty },
			outputs: new[] { Output } );

		ComponentBinding = NodeBinding.Create(
			DisplayInfo with
			{
				Title = "Component Reference",
				Description = "References a Component from the scene this graph is embedded in."
			},
			properties: new[] { ComponentProperty },
			outputs: new[]
			{
				Output with
				{
					Type = typeof(Component),
					Display = Output.Display with { Description = "The referenced Component." }
				}
			} );

		GameObjectBinding = NodeBinding.Create(
			DisplayInfo with
			{
				Title = "Game Object Reference",
				Description = "References a Game Object from the scene this graph is embedded in."
			},
			properties: new[] { GameObjectProperty },
			outputs: new[] { Output with
			{
				Type = typeof(GameObject),
				Display = Output.Display with
				{
					Description = "The referenced Game Object."
				}
			} } );

		InitLegacy();
	}

	private record Target(
		GameObjectReference GameObjectRef,
		ComponentReference? ComponentRef = null,
		GameObject? GameObject = null,
		Component? Component = null,
		bool IsLegacy = false )
	{
		public string? Name => IsComponent && Component is { } comp ? $"{comp.GameObject?.Name} \u2192 {comp.GetType().Name}" : GameObject?.Name;

		public bool IsGameObject => ComponentRef is null;
		public bool IsComponent => ComponentRef is not null;
	}

	private (NodeBinding Binding, Target? Target) BindTarget( BindingSurface surface )
	{
		if ( surface.Properties.TryGetValue( ComponentProperty.Name, out var compRefObj ) && compRefObj is ComponentReference compRef )
		{
			var compType = compRef.ResolveComponentType() ?? typeof( Component );
			var typeDesc = Game.TypeLibrary.GetType( compType );

			var desc = $"References a {typeDesc.Title} from the scene this graph is embedded in.";

			if ( !string.IsNullOrWhiteSpace( typeDesc.Description ) )
			{
				desc = $"{desc}<br/><br/>{typeDesc.Description}";
			}

			return (ComponentBinding with
			{
				DisplayInfo = ComponentBinding.DisplayInfo with
				{
					Title = $"{typeDesc.Title} Reference",
					Description = desc,
					Icon = typeDesc.Icon ?? ComponentBinding.DisplayInfo.Icon
				}

			}, new Target( (GameObjectReference)compRef, ComponentRef: compRef ));
		}

		if ( surface.Properties.TryGetValue( GameObjectProperty.Name, out var goRefObj ) && goRefObj is GameObjectReference goRef )
		{
			var binding = GameObjectBinding;

			if ( !string.IsNullOrWhiteSpace( goRef.PrefabPath ) )
			{
				binding = binding with
				{
					DisplayInfo = binding.DisplayInfo with
					{
						Title = "Prefab Reference",
						Description = "References a Game Object from a prefab file.",
						Icon = "ballot"
					}
				};
			}

			return (binding, new Target( goRef ));
		}

		if ( BindTargetLegacy( surface ) is { } legacyBinding )
		{
			return legacyBinding;
		}

		return (DefaultBinding, null);
	}

	protected override NodeBinding OnBind( BindingSurface surface )
	{
		var (binding, target) = BindTarget( surface );

		if ( target is null )
		{
			return binding;
		}

		if ( surface.ActionGraph?.GetEmbeddedTarget() is GameObject graphTarget )
		{
			try
			{
				target = target.ComponentRef is { } compRef
					? target with { Component = compRef.Resolve( graphTarget.Scene ) ?? throw new Exception( "Component not found in the same scene as this graph." ) }
					: target with { GameObject = target.GameObjectRef.Resolve( graphTarget.Scene ) ?? throw new Exception( "GameObject not found in the same scene as this graph." ) };
			}
			catch ( Exception ex )
			{
				binding = binding with
				{
					Messages = new[] { new NodeBinding.ValidationMessage( null, MessageLevel.Warning, ex.Message ) }
				};
			}

			if ( surface.Node is { } node && (target.GameObject is not null || target.Component is not null) )
			{
				graphTarget.SceneRefGizmo?.Register( node, target.GameObject ?? target.Component?.GameObject, target.Component );
			}
		}

		var refType = target.IsComponent
			? target.ComponentRef!.Value.ResolveComponentType() ?? typeof( Component )
			: typeof( GameObject );

		return binding with
		{
			DisplayInfo = binding.DisplayInfo with
			{
				Title = target.Name ?? binding.DisplayInfo.Title
			},
			Outputs = new[] { binding.Outputs.First() with { Type = refType } },
			Target = target
		};
	}

	private Expression ResolveReferenceExpression( INodeExpressionBuilder builder, Target target )
	{
		var getRef = builder.GetPropertyValue( target.IsGameObject ? GameObjectProperty : ComponentProperty );
		var resolveMethod = getRef.Type.GetMethod( nameof( GameObjectReference.Resolve ),
			BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes )!;
		return Expression.Call( getRef, resolveMethod );
	}

	protected override Expression OnBuildExpression( INodeExpressionBuilder builder )
	{
		var target = builder.GetBindingTarget<Target>();
		var resolveReference = target.IsLegacy
			? ResolveReferenceExpressionLegacy( builder, target )
			: ResolveReferenceExpression( builder, target );

		var assign = builder.GetOutputValue().Assign( target.IsComponent
			? Expression.Convert( resolveReference, builder.Node.Outputs.Values.First().Type )
			: resolveReference );

		return BuildDispatchTriggeredExpression( builder.Node ) is { } trigger
			? Expression.Block( trigger, assign )
			: assign;
	}
}
