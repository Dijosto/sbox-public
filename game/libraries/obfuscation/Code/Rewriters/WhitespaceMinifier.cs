namespace Obfuscation;

/// <summary>
/// Normalizes whitespace in the syntax tree - removes excess blank lines
/// and unnecessary indentation while preserving code validity.
/// </summary>
public class WhitespaceMinifier : CSharpSyntaxRewriter
{
	public WhitespaceMinifier() : base( visitIntoStructuredTrivia: false )
	{
	}

	public override SyntaxToken VisitToken( SyntaxToken token )
	{
		token = base.VisitToken( token );

		// Process leading trivia
		if ( token.HasLeadingTrivia )
		{
			var newLeading = ProcessTrivia( token.LeadingTrivia );
			token = token.WithLeadingTrivia( newLeading );
		}

		// Process trailing trivia
		if ( token.HasTrailingTrivia )
		{
			var newTrailing = ProcessTrivia( token.TrailingTrivia );
			token = token.WithTrailingTrivia( newTrailing );
		}

		return token;
	}

	private SyntaxTriviaList ProcessTrivia( SyntaxTriviaList triviaList )
	{
		var result = new List<SyntaxTrivia>();
		int consecutiveNewlines = 0;

		foreach ( var trivia in triviaList )
		{
			if ( trivia.IsKind( SyntaxKind.EndOfLineTrivia ) )
			{
				consecutiveNewlines++;
				// Only keep one newline max
				if ( consecutiveNewlines <= 1 )
				{
					result.Add( trivia );
				}
			}
			else if ( trivia.IsKind( SyntaxKind.WhitespaceTrivia ) )
			{
				// Reduce indentation - just keep minimal spacing
				var text = trivia.ToString();
				if ( text.Contains( '\t' ) || text.Length > 1 )
				{
					// Replace with single space if it's horizontal whitespace
					result.Add( SyntaxFactory.Space );
				}
				else
				{
					result.Add( trivia );
				}
				consecutiveNewlines = 0;
			}
			else
			{
				result.Add( trivia );
				consecutiveNewlines = 0;
			}
		}

		return SyntaxFactory.TriviaList( result );
	}

	/// <summary>
	/// Process a syntax tree, minifying whitespace.
	/// </summary>
	public static SyntaxTree Process( SyntaxTree tree )
	{
		var rewriter = new WhitespaceMinifier();
		var newRoot = rewriter.Visit( tree.GetRoot() );
		return tree.WithRootAndOptions( newRoot, tree.Options );
	}
}
