namespace Sandbox;

/// <summary>
/// Allows fast lookups of morph variables
/// </summary>
public sealed class ModelMorphs
{
	public Model Model { get; }

	public int Count { get; private set; }
	public string[] Names { get; internal set; }

	Dictionary<string, int> _nameToIndex = new( StringComparer.OrdinalIgnoreCase );
	Dictionary<int, string> _indexToName = new();

	internal ModelMorphs( Model model )
	{
		Model = model;

		Count = model.native.NumFlexControllers();

		for ( int i = 0; i < Count; i++ )
		{
			var name = model.native.GetFlexControllerName( i );
			_nameToIndex[name] = i;
			_indexToName[i] = name;
		}

		Names = _nameToIndex.Keys.ToArray();
	}

	/// <summary>
	/// Get the name of a morph by its index.
	/// </summary>
	public string GetName( int i ) => _indexToName.GetValueOrDefault( i );

	/// <summary>
	/// Get the index of a morph by its name
	/// </summary>
	public int GetIndex( string name ) => _nameToIndex.GetValueOrDefault( name );

	/// <summary>
	/// Clear it so it can't be used after disposed
	/// </summary>
	internal void Dispose()
	{
		Count = 0;

		_nameToIndex.Clear();
		_nameToIndex = default;

		_indexToName.Clear();
		_indexToName = default;
	}
}

public partial class Model
{
	ModelMorphs _morphs;

	/// <summary>
	/// Access to bones of this model.
	/// </summary>
	public ModelMorphs Morphs
	{
		get
		{
			_morphs ??= new ModelMorphs( this );
			return _morphs;
		}
	}

	/// <summary>
	/// Number of morph controllers this model has.
	/// </summary>
	public int MorphCount => Morphs.Count;


	/// <summary>
	/// Returns name of a morph controller at given index.
	/// </summary>
	/// <param name="morph">Morph controller index to get name of, starting at 0.</param>
	/// <returns>Name of the morph controller at given index.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when given index exceeds range of [0,MorphCount-1]</exception>
	public string GetMorphName( int morph )
	{
		if ( morph < 0 || morph >= MorphCount )
			throw new ArgumentOutOfRangeException( nameof( morph ), $"Tried to access out of range morph index {morph}, range is 0-{MorphCount - 1}" );
		return native.GetFlexControllerName( morph );
	}

	/// <summary>
	/// Get morph weight for viseme.
	/// </summary>
	public float GetVisemeMorph( string viseme, int morph )
	{
		if ( morph < 0 || morph >= MorphCount )
			throw new ArgumentOutOfRangeException( nameof( morph ), $"Tried to access out of range morph index {morph}, range is 0-{MorphCount - 1}" );

		return native.GetVisemeMorph( viseme, morph );
	}
}
