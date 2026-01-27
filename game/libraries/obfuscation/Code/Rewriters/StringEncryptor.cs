namespace Obfuscation;

/// <summary>
/// Encrypts string literals by replacing them with decryption calls.
/// This makes it harder to find strings in the compiled code.
/// </summary>
public class StringEncryptor : CSharpSyntaxRewriter
{
	private readonly List<(string Original, string Encrypted)> _strings = new();
	private readonly byte[] _key;
	private bool _collectMode = true;

	public StringEncryptor( byte[] key = null )
	{
		// Use a default key or provided key
		_key = key ?? new byte[] { 0x5A, 0x3B, 0x9C, 0x2D, 0x7E, 0x4F, 0x1A, 0x8B };
	}

	private string XorEncrypt( string input )
	{
		var bytes = Encoding.UTF8.GetBytes( input );
		for ( int i = 0; i < bytes.Length; i++ )
		{
			bytes[i] ^= _key[i % _key.Length];
		}
		return Convert.ToBase64String( bytes );
	}

	private bool ShouldEncrypt( LiteralExpressionSyntax literal )
	{
		// Don't encrypt empty strings or very short strings
		var value = literal.Token.ValueText;
		if ( string.IsNullOrEmpty( value ) || value.Length < 3 )
			return false;

		// Don't encrypt strings that are used in attributes
		if ( literal.Parent is AttributeArgumentSyntax )
			return false;

		// Don't encrypt interpolated string parts
		if ( literal.Parent is InterpolatedStringTextSyntax )
			return false;

		// Don't encrypt nameof results (they're compile-time)
		if ( literal.Parent is InvocationExpressionSyntax inv &&
			 inv.Expression is IdentifierNameSyntax id &&
			 id.Identifier.Text == "nameof" )
			return false;

		return true;
	}

	public override SyntaxNode VisitLiteralExpression( LiteralExpressionSyntax node )
	{
		if ( node.Kind() != SyntaxKind.StringLiteralExpression )
			return base.VisitLiteralExpression( node );

		if ( !ShouldEncrypt( node ) )
			return base.VisitLiteralExpression( node );

		var originalValue = node.Token.ValueText;

		if ( _collectMode )
		{
			var encrypted = XorEncrypt( originalValue );
			_strings.Add( (originalValue, encrypted) );
			return base.VisitLiteralExpression( node );
		}
		else
		{
			// Replace with decryption call
			var encrypted = XorEncrypt( originalValue );
			var keyString = Convert.ToBase64String( _key );

			// Create: StringDecryptor.Decrypt("encrypted", "key")
			var invocation = SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName( "StringDecryptor" ),
					SyntaxFactory.IdentifierName( "Decrypt" ) ),
				SyntaxFactory.ArgumentList(
					SyntaxFactory.SeparatedList( new[]
					{
						SyntaxFactory.Argument(
							SyntaxFactory.LiteralExpression(
								SyntaxKind.StringLiteralExpression,
								SyntaxFactory.Literal( encrypted ) ) ),
						SyntaxFactory.Argument(
							SyntaxFactory.LiteralExpression(
								SyntaxKind.StringLiteralExpression,
								SyntaxFactory.Literal( keyString ) ) )
					} ) ) )
				.WithTriviaFrom( node );

			return invocation;
		}
	}

	/// <summary>
	/// Generate the decryptor helper class that needs to be included in the output.
	/// </summary>
	public static string GenerateDecryptorClass()
	{
		return @"
namespace Obfuscation
{
	internal static class StringDecryptor
	{
		public static string Decrypt( string encrypted, string keyBase64 )
		{
			var bytes = System.Convert.FromBase64String( encrypted );
			var key = System.Convert.FromBase64String( keyBase64 );
			for ( int i = 0; i < bytes.Length; i++ )
			{
				bytes[i] ^= key[i % key.Length];
			}
			return System.Text.Encoding.UTF8.GetString( bytes );
		}
	}
}
";
	}

	/// <summary>
	/// Process a syntax tree, encrypting string literals.
	/// Returns the transformed tree and a helper tree with the decryptor.
	/// </summary>
	public static (SyntaxTree TransformedTree, SyntaxTree HelperTree) Process( SyntaxTree tree, CSharpParseOptions options = null )
	{
		options ??= CSharpParseOptions.Default;

		var rewriter = new StringEncryptor();

		// First pass: collect strings
		rewriter._collectMode = true;
		rewriter.Visit( tree.GetRoot() );

		// Second pass: transform
		rewriter._collectMode = false;
		var newRoot = rewriter.Visit( tree.GetRoot() );
		var transformedTree = tree.WithRootAndOptions( newRoot, tree.Options );

		// Generate helper class
		var helperTree = CSharpSyntaxTree.ParseText(
			GenerateDecryptorClass(),
			options,
			path: "__StringDecryptor.cs" );

		return (transformedTree, helperTree);
	}

	/// <summary>
	/// Process multiple syntax trees with string encryption.
	/// Returns all trees plus one helper tree with the decryptor.
	/// </summary>
	public static List<SyntaxTree> ProcessAll( IEnumerable<SyntaxTree> trees, CSharpParseOptions options = null )
	{
		options ??= CSharpParseOptions.Default;
		var rewriter = new StringEncryptor();
		var result = new List<SyntaxTree>();

		// First pass: collect all strings
		rewriter._collectMode = true;
		foreach ( var tree in trees )
		{
			rewriter.Visit( tree.GetRoot() );
		}

		// Second pass: transform all trees
		rewriter._collectMode = false;
		foreach ( var tree in trees )
		{
			var newRoot = rewriter.Visit( tree.GetRoot() );
			result.Add( tree.WithRootAndOptions( newRoot, tree.Options ) );
		}

		// Add helper class if any strings were encrypted
		if ( rewriter._strings.Count > 0 )
		{
			var helperTree = CSharpSyntaxTree.ParseText(
				GenerateDecryptorClass(),
				options,
				path: "__StringDecryptor.cs" );
			result.Add( helperTree );
		}

		return result;
	}
}
