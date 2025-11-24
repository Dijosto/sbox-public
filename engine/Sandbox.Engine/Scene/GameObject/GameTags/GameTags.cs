namespace Sandbox;
/// <summary>
/// Entity Tags are strings you can set and check for on any entity. Internally
/// these strings are tokenized and networked so they're also available clientside.
/// </summary>
[Expose, ActionGraphIgnore]
public class GameTags : ITagSet
{
	private HashSet<uint> _tokens = new();
	HashSet<string> _tags = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

	GameObject target;

	internal GameTags( GameObject target )
	{
		this.target = target;
	}

	/// <summary>
	/// Returns all the tags this object has.
	/// </summary>
	public override IEnumerable<string> TryGetAll()
	{
		if ( target.Parent is null || target.Parent is Scene )
			return _tags;

		return _tags.Concat( target.Parent.Tags.TryGetAll() ).Distinct();
	}

	/// <summary>
	/// Returns all the tags this object has.
	/// </summary>
	[Pure, ActionGraphInclude]
	public IEnumerable<string> TryGetAll( bool includeAncestors )
	{
		if ( !includeAncestors ) return _tags;
		return TryGetAll();
	}

	/// <summary>
	/// Returns true if this object (or its parents) has given tag.
	/// </summary>
	public override bool Has( string tag )
	{
		if ( _tags.Contains( tag ) )
			return true;

		return target.Parent?.Tags.Has( tag ) ?? false;
	}

	/// <summary>
	/// Returns true if this object has given tag.
	/// </summary>
	[Pure, ActionGraphInclude]
	public bool Has( string tag, bool includeAncestors )
	{
		if ( !includeAncestors ) return _tags.Contains( tag );
		return Has( tag );
	}

	/// <summary>
	/// Returns true if this object has one or more tags from given tag list.
	/// </summary>
	public bool HasAny( HashSet<string> tagList )
	{
		return tagList.Any( Has );
	}

	bool AddSingle( string tag )
	{
		if ( string.IsNullOrWhiteSpace( tag ) ) return false;
		if ( Has( tag ) ) return false;

		tag = tag.ToLowerInvariant();

		if ( !tag.IsValidTag() )
		{
			Log.Warning( $"Ignoring tag '{tag}' - invalid" );
			return false;
		}

		_tokens.Add( StringToken.FindOrCreate( tag ) );

		return _tags.Add( tag );
	}

	/// <summary>
	/// Try to add the tag to this object.
	/// </summary>
	public override void Add( string tag )
	{
		if ( AddSingle( tag ) )
		{
			MarkDirty();
		}
	}

	/// <summary>
	/// Adds multiple tags. Calls <see cref="Add(string)">EntityTags.Add</see> for each tag.
	/// </summary>
	[ActionGraphInclude]
	public void Add( params string[] tags )
	{
		if ( tags == null || tags.Length == 0 )
			return;

		bool changes = false;

		foreach ( var tag in tags )
		{
			changes = AddSingle( tag ) || changes;
		}

		if ( changes )
		{
			MarkDirty();
		}
	}

	/// <summary>
	/// Try to remove the tag from this entity.
	/// </summary>
	[ActionGraphInclude]
	public override void Remove( string tag )
	{
		if ( !_tags.Remove( tag ) )
			return;

		_tokens.Remove( StringToken.FindOrCreate( tag ) );

		MarkDirty();
	}

	/// <summary>
	/// Remove all tags
	/// </summary>
	[ActionGraphInclude]
	public override void RemoveAll()
	{
		_tokens.Clear();
		_tags.Clear();

		MarkDirty();
	}

	internal void SetAll( string tags )
	{
		RemoveAll();
		Add( tags.SplitQuotesStrings() );
	}

	void MarkDirty()
	{
		if ( !target.IsValid )
			return;

		target.OnTagsUpdatedInternal();

		// make all our children dirty too
		foreach ( var c in target.Children )
		{
			c.Tags.MarkDirty();
		}
	}


	[System.Obsolete( "No need to call this now, tags are set immediately" )]
	public void Flush()
	{

	}

	/// <summary>
	/// Returns a list of ints, representing the tags. These are used internally by the engine.
	/// </summary>
	public override IReadOnlySet<uint> GetTokens() => _tokens;

	/// <summary>
	/// Get all potential suggested tags that someone might want to add to this set.
	/// </summary>
	public override IEnumerable<string> GetSuggested()
	{
		var collisionTags = ProjectSettings.Collision?.Tags ?? Enumerable.Empty<string>();
		var sceneTags = target.IsValid()
			? target.Scene.GetAllObjects( true )
				.SelectMany( x => x.Tags.TryGetAll() ?? Enumerable.Empty<string>() )
			: Enumerable.Empty<string>();

		return collisionTags
			.Concat( sceneTags )
			.Distinct();
	}
}
