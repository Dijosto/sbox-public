namespace Sandbox;

public sealed class MaterialGroupBuilder
{
	/// <summary>
	/// The name of the material group.
	/// </summary>
	public string Name { get; internal set; }

	internal List<Material> Materials = [];

	/// <inheritdoc cref="Name"/>
	public MaterialGroupBuilder WithName( string name )
	{
		Name = name;
		return this;
	}

	/// <summary>
	/// Add a material to the group.
	/// </summary>
	public MaterialGroupBuilder AddMaterial( Material material )
	{
		Materials.Add( material );
		return this;
	}

	/// <summary>
	/// Add a materials to the group.
	/// </summary>
	public MaterialGroupBuilder AddMaterials( Span<Material> materials )
	{
		Materials.AddRange( materials );
		return this;
	}

	internal MaterialGroupBuilder()
	{
	}
}

partial class ModelBuilder
{
	private readonly List<MaterialGroupBuilder> _materialGroups = [];

	/// <summary>
	/// Add a named material group builder.
	/// </summary>
	public MaterialGroupBuilder AddMaterialGroup( string name )
	{
		var builder = new MaterialGroupBuilder();
		builder.Name = name;
		_materialGroups.Add( builder );
		return builder;
	}

	private CBuilderMaterialGroupArray CreateMaterialGroups()
	{
		var result = CBuilderMaterialGroupArray.Create( _materialGroups.Count );

		for ( int i = 0; i < _materialGroups.Count; i++ )
		{
			var src = _materialGroups[i];
			var dst = result.Get( i );
			dst.m_name = src.Name;

			foreach ( var mat in src.Materials )
			{
				if ( mat.native.IsNull ) continue;
				dst.AddMaterial( mat.native );
			}
		}

		return result;
	}
}
