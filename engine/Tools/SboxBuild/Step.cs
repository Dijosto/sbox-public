using static Facepunch.Constants;

namespace Facepunch.Steps;

internal class Step( string name )
{
	public string Name { get; init; } = name;

	public ExitCode Run()
	{
		return RunInternal();
	}

	protected virtual ExitCode RunInternal()
	{
		return ExitCode.Failure;
	}
}

/// <summary>
/// Allows to group steps together so the pipeline is less verbose.
/// </summary>
internal class StepGroup( string name, IReadOnlyList<Step> steps, bool continueOnFailure = false ) : Step( name )
{
	public IReadOnlyList<Step> Steps { get; } = steps;
	public bool ContinueOnFailure { get; } = continueOnFailure;

	protected override ExitCode RunInternal()
	{
		var failedSteps = new List<Step>();

		foreach ( var step in Steps )
		{
			var result = step.Run();
			if ( result != ExitCode.Success )
			{
				failedSteps.Add( step );
				if ( !ContinueOnFailure )
					break;
			}
		}

		if ( failedSteps.Count > 0 )
		{
			foreach ( var failed in failedSteps )
				Log.Error( $"StepGroup '{Name}': Step '{failed.Name}' failed." );
			return ExitCode.Failure;
		}

		return ExitCode.Success;
	}
}
