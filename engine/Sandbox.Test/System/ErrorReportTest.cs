using Sandbox.Engine;

namespace TestSystem;

[TestClass]
public class ErrorReportTest
{
	[TestMethod]
	public void BasicReport()
	{
		ErrorReporter.Initialize();

		try
		{
			throw new System.Exception( "Unit Test Exception" );
		}
		catch ( System.Exception e )
		{
			ErrorReporter.ReportException( e );
		}

		ErrorReporter.Flush();
	}
}
