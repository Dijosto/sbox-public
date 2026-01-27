namespace Obfuscation;

/// <summary>
/// Configuration options for the obfuscation pipeline.
/// </summary>
public class ObfuscationOptions
{
	/// <summary>
	/// Remove all comments from the code.
	/// </summary>
	public bool StripComments { get; set; } = true;

	/// <summary>
	/// Rename private fields, methods, and local variables.
	/// </summary>
	public bool RenamePrivateSymbols { get; set; } = true;

	/// <summary>
	/// Encrypt string literals.
	/// </summary>
	public bool EncryptStrings { get; set; } = false;

	/// <summary>
	/// Minify whitespace (remove excess blank lines and indentation).
	/// </summary>
	public bool MinifyWhitespace { get; set; } = true;

	/// <summary>
	/// Names to preserve from renaming (e.g., serialized field names).
	/// </summary>
	public HashSet<string> PreservedNames { get; set; } = new();
}

/// <summary>
/// Main pipeline that orchestrates all obfuscation transforms.
/// </summary>
public static class ObfuscationPipeline
{
	/// <summary>
	/// Apply obfuscation to a collection of syntax trees.
	/// </summary>
	public static List<SyntaxTree> Process( IEnumerable<SyntaxTree> trees, ObfuscationOptions options = null )
	{
		options ??= new ObfuscationOptions();
		var result = trees.ToList();

		// Step 1: Strip comments
		if ( options.StripComments )
		{
			result = result.Select( t => CommentStripper.Process( t ) ).ToList();
		}

		// Step 2: Rename private symbols
		if ( options.RenamePrivateSymbols )
		{
			result = SymbolRenamer.ProcessAll( result, options.PreservedNames );
		}

		// Step 3: Encrypt strings
		if ( options.EncryptStrings )
		{
			var parseOptions = result.FirstOrDefault()?.Options as CSharpParseOptions;
			result = StringEncryptor.ProcessAll( result, parseOptions );
		}

		// Step 4: Minify whitespace (do this last to clean up any artifacts)
		if ( options.MinifyWhitespace )
		{
			result = result.Select( t => WhitespaceMinifier.Process( t ) ).ToList();
		}

		return result;
	}

	/// <summary>
	/// Apply obfuscation to a CodeArchive's syntax trees in place.
	/// </summary>
	public static void ProcessArchive( CodeArchive archive, ObfuscationOptions options = null )
	{
		var obfuscated = Process( archive.SyntaxTrees, options );
		archive.SyntaxTrees.Clear();
		archive.SyntaxTrees.AddRange( obfuscated );
	}
}
