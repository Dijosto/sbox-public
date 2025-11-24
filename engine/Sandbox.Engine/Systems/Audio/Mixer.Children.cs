namespace Sandbox.Audio;

public partial class Mixer
{
	List<Mixer> Children = new List<Mixer>();

	public Mixer AddChild()
	{
		lock ( Lock )
		{
			var m = new Mixer( this );
			m.Name = "Child Mixer";
			Children.Add( m );
			return m;
		}
	}

	public void Destroy()
	{
		if ( Parent is null )
			return;

		lock ( Parent.Lock )
		{
			Parent.Children.Remove( this );
		}
	}

	public Mixer[] GetChildren()
	{
		lock ( Lock )
		{
			return Children.ToArray();
		}
	}

	[Hide]
	public int ChildCount => Children.Count;
}
