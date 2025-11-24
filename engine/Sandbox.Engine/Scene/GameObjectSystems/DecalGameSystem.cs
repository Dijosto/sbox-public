namespace Sandbox;

public sealed class DecalGameSystem : GameObjectSystem<DecalGameSystem>
{
	[ConVar( "maxdecals" )]
	public static int MaxDecals { get; internal set; } = 1000;

	/// <summary>
	/// A list of decals that can be destroyed after a certain time.
	/// </summary>
	LinkedList<Decal> _transients = new();

	public DecalGameSystem( Scene scene ) : base( scene )
	{

	}

	public void ClearDecals()
	{
		while ( _transients.Count > 0 )
		{
			var first = _transients.First;
			first.Value.Destroy();

			_transients.Remove( first );
		}
	}

	internal void AddTransient( Decal decal )
	{
		if ( decal is null || !decal.IsValid() )
			return;

		_transients.AddLast( decal );

		int max = MaxDecals;
		while ( _transients.Count > max )
		{
			var first = _transients.First;
			first.Value.Destroy();

			_transients.Remove( first );
		}
	}

	internal void RemoveTransient( Decal decal )
	{
		if ( decal is null ) return;

		_transients.Remove( decal );
	}
}
