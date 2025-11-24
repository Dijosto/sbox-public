using System.Text.Json.Nodes;
using Facepunch.ActionGraphs;

namespace Sandbox;

public abstract partial class Component
{
	// Set only during the cloning process
	// We store this on the component to avoid the need reverse lookup table during the clone process
	private Component _cloneOriginal = null;

	/// <summary>
	/// Runs after this clone has been created by a cloned GameObject.
	/// </summary>
	/// <param name="original">The original component that was cloned.</param>
	/// <param name="originalToClonedObject">During the cloning process, we build a mapping from original objects to their clone, so we will need to add ourselves to it.</param>
	internal void InitClone( Component original, Dictionary<object, object> originalToClonedObject )
	{
		originalToClonedObject[original] = this;
		_cloneOriginal = original;
	}

	/// <summary>
	/// Runs after all objects of the original hierarchy have been cloned/created.
	/// Here we initialize the clones properties and fields with the values from the original object.
	/// <param name="originalToClonedObject">A mapping of original objects to their clones, used for all reference types.</param>
	/// <param name="originalIdToCloneId">A mapping of original GUIDs to cloned GUIDs, used for GameObject and Component references in JSON.</param>
	/// </summary>
	internal void PostClone( Dictionary<object, object> originalToClonedObject, Dictionary<Guid, Guid> originalIdToCloneId )
	{
		if ( !_cloneOriginal.IsValid() )
		{
			// Nothing todo this component is not a proper clone. It was created through side effects while cloning properties.
			return;
		}

		using var targetScope = ActionGraph.PushTarget( InputDefinition.Target( typeof( GameObject ), GameObject ) );

		ClonePropertiesAndFields( _cloneOriginal, originalToClonedObject, originalIdToCloneId );

		CheckRequireComponent();

		_cloneOriginal = null;
	}

	private void ClonePropertiesAndFields( object original, Dictionary<object, object> originalToClonedObject, Dictionary<Guid, Guid> originalIdToCloneId )
	{
		foreach ( var member in ReflectionQueryCache.OrderedSerializableMembers( GetType() ) )
		{
			CloneHelpers.CloneMember(
				this,
				original,
				member,
				originalToClonedObject,
				originalIdToCloneId );
		}
	}
}

/// <summary>
/// Provides helper methods for cloning objects and their members.
/// We use a heuristic <see cref="ReflectionQueryCache.IsTypeCloneableByCopy"/> to determine if a type can be cloned by copy to speed up cloning.
/// If we cannot copy something and we have to "clone" we do so by serializing to and deserializing from JSON.
/// However, our goal is to copy as much as possible to avoid the serialization overhead.
/// </summary>
internal static class CloneHelpers
{
	public static void CloneMember(
		object target,
		object original,
		MemberDescription member,
		Dictionary<object, object> originalToClonedObject,
		Dictionary<Guid, Guid> originalIdToCloneId )
	{
		object originalValue = null;
		Type valueType = null;

		if ( member is PropertyDescription prop )
		{
			originalValue = prop.GetValue( original );
			valueType = prop.PropertyType;
		}
		else if ( member is FieldDescription field )
		{
			originalValue = field.GetValue( original );
			valueType = field.FieldType;
		}
		else
		{
			throw new InvalidOperationException( "Member is neither a property nor a field" );
		}

		if ( originalValue is null || ReflectionQueryCache.IsTypeCloneableByCopy( valueType ) )
		{
			SetMemberValue( member, target, originalValue );
			return;
		}

		// If the original object has already been cloned simply point to it.
		// For now only do this for Component and GameObjects ( matches original clone via JSON behaviour )
		var isGameObjectorComponent = (originalValue is GameObject || originalValue is Component);
		// There is an ambiguity when we reference the root of the prefab, it could either mean we want to reference the cloned root or the original prefab.
		// To maintain old clone behaviour we reference the cloned root gameobject except when the cloned property is of type PrefabScene, in that case we reference the original prefab.
		var isPrefabReference = originalValue is PrefabScene && valueType == typeof( PrefabScene );
		if ( isGameObjectorComponent && !isPrefabReference && originalToClonedObject.TryGetValue( originalValue, out var existingClone ) )
		{
			SetMemberValue( member, target, existingClone );
			return;
		}

		// Fallback to JSON
		var clonedJson = Json.ToNode( originalValue, valueType );
		UpdateClonedIdsInJson( clonedJson, originalIdToCloneId );

		var targetValue = member is PropertyDescription ? ((PropertyDescription)member).GetValue( target ) : ((FieldDescription)member).GetValue( target );

		if ( targetValue is IJsonPopulator jsonPopulator )
		{
			if ( targetValue == null )
				targetValue = Activator.CreateInstance( valueType );

			jsonPopulator.Deserialize( clonedJson );

			SetMemberValue( member, target, targetValue );
		}
		else
		{
			var clonedValue = Json.FromNode( clonedJson, valueType );

			SetMemberValue( member, target, clonedValue );
		}
	}

	private static void SetMemberValue(
		MemberDescription member,
		object target,
		object value )
	{
		if ( member is PropertyDescription prop )
		{
			prop.SetValue( target, value );
		}
		else if ( member is FieldDescription field )
		{
			field.SetValue( target, value );
		}
	}

	/// <summary>
	/// We want GUIDS that reference something within the original hierarchy to reference the corresponding clone in the new hierarchy.
	/// </summary>
	public static void UpdateClonedIdsInJson( in JsonNode json, Dictionary<Guid, Guid> originalIdToCloneId )
	{
		Sandbox.Json.WalkJsonTree( json, ( k, v ) =>
		{
			if ( !v.TryGetValue<Guid>( out var guid ) ) return v;
			if ( !originalIdToCloneId.TryGetValue( guid, out var updatedGuid ) ) return v;

			return updatedGuid;
		} );
	}
}
