using Refit;

namespace Sandbox.Services;

public partial class ServiceApi
{
	public interface IVersionApi
	{
		[Get( "/package/versions/{packageIdent}" )]
		Task<PackageVersion[]> GetList( string packageIdent );
	}
}
