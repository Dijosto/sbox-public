namespace Sandbox;

internal static partial class ConVarSystem
{
	public static ConCmdAttribute.AutoCompleteResult[] GetAutoComplete( string partial, int count )
	{
		var parts = partial.SplitQuotesStrings();

		List<ConCmdAttribute.AutoCompleteResult> results = new();

		//
		// if we have more than one part, complete a specific command
		//
		if ( parts.Length > 1 )
		{
			if ( !Members.TryGetValue( parts[0], out var command ) )
				return Array.Empty<ConCmdAttribute.AutoCompleteResult>();

			//results.Add( new ConCmd.AutoCompleteResult { Command = command.Name, Description = command.Help } );

			// TODO - dig into it for auto complete

			return results.Take( count ).ToArray();
		}

		//
		// Find the command starting with this
		//

		foreach ( var option in Members.Values
											.Where( x => !x.IsHidden )
											.Where( x => x.Name.StartsWith( partial, StringComparison.OrdinalIgnoreCase ) )
											.OrderBy( x => x.Name ) )
		{

			if ( option.Name == partial )
				continue;

			results.Add( new ConCmdAttribute.AutoCompleteResult
			{
				Command = option.Name,
				Description = option.BuildDescription(),
			} );
		}

		return results.Take( count ).ToArray();
	}
}
