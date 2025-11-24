namespace Facepunch.InteropGen;

internal class CodeWriter
{
	public int Indent { get; set; }
	public System.Text.StringBuilder sb { get; protected set; } = new System.Text.StringBuilder();
	public bool Empty => sb.Length == 0;

	public void Write( string line, bool indent = false )
	{
		if ( indent )
		{
			_ = sb.Append( new string( '\t', Indent ) );
		}

		_ = sb.Append( line );
	}

	public void WriteLine( string line = "" )
	{
		_ = sb.Append( new string( '\t', Indent ) );
		_ = sb.AppendLine( line );
	}

	public void WriteLines( string text )
	{
		string[] lines = text.Split( '\n' );

		foreach ( string line in lines )
		{
			WriteLine( line.TrimEnd() );
		}
	}

	public void StartBlock( string line )
	{
		WriteLine( line );
		WriteLine( "{" );

		Indent++;
	}

	public void EndBlock( string line = "" )
	{
		Indent--;
		WriteLine( $"}}{line}" );
	}

	public override string ToString()
	{
		return sb.ToString();
	}
}
