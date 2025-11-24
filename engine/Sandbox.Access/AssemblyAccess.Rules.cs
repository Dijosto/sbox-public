using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Sandbox;

internal partial class AssemblyAccess
{
	bool CheckPassesRules()
	{
		Parallel.ForEach( Touched, touch =>
		{
			if ( Global.Rules.IsInWhitelist( touch.Key ) )
				return;

			var locations = string.Join( "\n", touch.Value.Locations.Select( x => $"\t{x.Text}" ) );

			Result.Errors.Add( $"{touch.Key}\n{locations}" );
			Result.WhitelistErrors.Add( (touch.Key, touch.Value.Locations.ToArray()) );
		} );

		return Result.Errors.Count == 0;
	}
}
