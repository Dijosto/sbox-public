using System.Text;

namespace Facepunch;

/// <summary>
/// Captures console output while maintaining a buffer of the last N lines
/// </summary>
public class ConsoleOutputCapture : TextWriter
{
	private readonly TextWriter originalOut;
	private readonly int maxLines;
	private readonly List<string> outputBuffer;
	private StringBuilder currentLine = new StringBuilder();

	public ConsoleOutputCapture( TextWriter originalOut, int maxLines = 100 )
	{
		this.originalOut = originalOut;
		this.maxLines = maxLines;
		this.outputBuffer = new List<string>( maxLines );
	}

	public override Encoding Encoding => originalOut.Encoding;

	public override void Write( char value )
	{
		// Write to the original output
		originalOut.Write( value );

		// Capture for our buffer
		if ( value == '\n' )
		{
			// Line completed, add to buffer
			AddToBuffer( currentLine.ToString() );
			currentLine.Clear();
		}
		else if ( value != '\r' ) // Ignore carriage returns
		{
			currentLine.Append( value );
		}
	}

	public override void Write( string value )
	{
		if ( string.IsNullOrEmpty( value ) )
		{
			originalOut.Write( value );
			return;
		}

		// Write to the original output
		originalOut.Write( value );

		// Process the string for our buffer
		ProcessString( value );
	}

	public override void WriteLine( string value )
	{
		// Write to the original output
		originalOut.WriteLine( value );

		// Process the string and add a line
		ProcessString( value );
		AddToBuffer( currentLine.ToString() );
		currentLine.Clear();
	}

	private void ProcessString( string value )
	{
		int start = 0;
		int newlinePos;

		// Process any complete lines in the string
		while ( (newlinePos = value.IndexOf( '\n', start )) != -1 )
		{
			string line = value.Substring( start, newlinePos - start );

			// Trim carriage return if present
			if ( line.EndsWith( '\r' ) )
			{
				line = line.Substring( 0, line.Length - 1 );
			}

			// Add current buffer + this segment as a line
			AddToBuffer( currentLine + line );
			currentLine.Clear();

			start = newlinePos + 1;
		}

		// Add any remaining part to the current line buffer
		if ( start < value.Length )
		{
			currentLine.Append( value.Substring( start ) );
		}
	}

	private void AddToBuffer( string line )
	{
		outputBuffer.Add( line );

		// Keep only the last maxLines
		if ( outputBuffer.Count > maxLines )
		{
			outputBuffer.RemoveAt( 0 );
		}
	}

	/// <summary>
	/// Get the captured output as a string with each line separated by newlines
	/// </summary>
	public string GetCapturedOutput()
	{
		return string.Join( "\n", outputBuffer );
	}

	/// <summary>
	/// Get the last n lines of captured output
	/// </summary>
	/// <param name="lineCount">Number of lines to return</param>
	/// <returns>String containing the last n lines</returns>
	public string GetLastLines( int lineCount )
	{
		if ( lineCount <= 0 || outputBuffer.Count == 0 )
			return string.Empty;

		int startIndex = Math.Max( 0, outputBuffer.Count - lineCount );
		return string.Join( "\n", outputBuffer.Skip( startIndex ) );
	}

	/// <summary>
	/// Check if the buffer has any content
	/// </summary>
	public bool HasContent => outputBuffer.Count > 0;
}
