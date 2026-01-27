namespace Obfuscation;

/// <summary>
/// Removes all comments from a syntax tree while preserving necessary whitespace.
/// </summary>
public class CommentStripper : CSharpSyntaxRewriter
{
	public CommentStripper() : base( visitIntoStructuredTrivia: true )
	{
	}

	public override SyntaxTrivia VisitTrivia( SyntaxTrivia trivia )
	{
		// Remove all comment types
		switch ( trivia.Kind() )
		{
			case SyntaxKind.SingleLineCommentTrivia:
			case SyntaxKind.MultiLineCommentTrivia:
			case SyntaxKind.SingleLineDocumentationCommentTrivia:
			case SyntaxKind.MultiLineDocumentationCommentTrivia:
			case SyntaxKind.DocumentationCommentExteriorTrivia:
				return default;
		}

		return base.VisitTrivia( trivia );
	}

	/// <summary>
	/// Process a syntax tree, removing all comments.
	/// </summary>
	public static SyntaxTree Process( SyntaxTree tree )
	{
		var rewriter = new CommentStripper();
		var newRoot = rewriter.Visit( tree.GetRoot() );
		return tree.WithRootAndOptions( newRoot, tree.Options );
	}
}
