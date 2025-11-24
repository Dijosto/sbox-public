using System.Text;

namespace Facepunch.InteropGen.Parsers;

internal class BodyParser : BaseParser
{
	private readonly Function Func;
	public bool IsNative { get; set; }

	public BodyParser( Definition definition, Function f )
	{
		this.definition = definition;
		Func = f;
	}

	private int Scopes = 0;

	public override void ParseLine( string line )
	{
		if ( line.Trim() == "{" )
		{
			Scopes++;

			if ( Scopes == 1 )
			{
				Func.Body = new StringBuilder();
				return;
			}
		}

		if ( line.Trim() == "}" )
		{
			Scopes--;

			if ( Scopes == 0 )
			{
				Finished = true;
				return;
			}
		}

		_ = Func.Body.AppendLine( line );
	}

}
