using Facepunch.Steps;
using static Facepunch.Constants;

namespace Facepunch.Pipelines;

internal class FormatAll
{
	public static Pipeline Create( bool verifyOnly )
	{
		var builder = new PipelineBuilder( "Format All" );

		// Add format steps for all solutions
		builder.AddStep( new Format( "Format Engine", Solutions.Engine, Format.Mode.Full, verifyOnly ) );
		builder.AddStep( new Format( "Format Editor", Solutions.Toolbase, Format.Mode.Whitespace, verifyOnly ) );
		builder.AddStep( new Format( "Format Menu", Solutions.Menu, Format.Mode.Whitespace, verifyOnly ) );
		builder.AddStep( new Format( "Format Build Tools", Solutions.BuildTools, Format.Mode.Full, verifyOnly ) );

		return builder.Build();
	}
}
