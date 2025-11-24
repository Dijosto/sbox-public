using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestPackage;

public class Program : TestCompiler.IProgram
{
	public int Main( StringWriter output )
	{
		output.Write( string.Join( ", ", Enumerable.Range( 0, 10 ).Select( x => x.ToString() ) ) );

		return 0;
	}

}
