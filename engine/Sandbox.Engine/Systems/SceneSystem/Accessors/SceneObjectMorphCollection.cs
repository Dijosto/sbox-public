using Sandbox;

/// <summary>
/// Hidden class. addon code should only ever access MorphCollection.
/// </summary>
internal sealed class SceneObjectMorphCollection : MorphCollection
{
	readonly SceneModel Target;

	public SceneObjectMorphCollection( SceneModel sceneObject )
	{
		Target = sceneObject;
	}

	public override void ResetAll()
	{
		Target.animNative.SBox_ClearFlexOverride();
	}

	public override void Reset( int i )
	{
		Target.animNative.SBox_SetFlexOverride( i, 0.0f );
	}

	public override void Reset( string name )
	{
		Target.animNative.SBox_SetFlexOverride( name, 0.0f );
	}

	public override void Set( int i, float weight )
	{
		Target.animNative.SBox_SetFlexOverride( i, weight );
	}

	public override void Set( string name, float weight )
	{
		Target.animNative.SBox_SetFlexOverride( name, weight );
	}

	public override float Get( int i )
	{
		return Target.animNative.SBox_GetFlexOverride( i );
	}

	public override float Get( string name )
	{
		return Target.animNative.SBox_GetFlexOverride( name );
	}

	public override string GetName( int index )
	{
		return Target.Model.GetMorphName( index );
	}

	public override int Count => Target.Model.MorphCount;
}
