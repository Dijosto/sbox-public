namespace Obfuscation;

/// <summary>
/// Renames private members and local variables to obfuscated names.
/// Uses a consistent naming scheme so references remain valid.
/// </summary>
public class SymbolRenamer : CSharpSyntaxRewriter
{
	private readonly Dictionary<string, string> _nameMap = new();
	private readonly HashSet<string> _preservedNames;
	private int _counter = 0;

	private static readonly HashSet<string> ReservedNames = new()
	{
		// C# keywords that could cause issues
		"value", "var", "this", "base", "true", "false", "null",
		"get", "set", "add", "remove", "partial", "async", "await",
		"nameof", "typeof", "sizeof", "default"
	};

	public SymbolRenamer( HashSet<string> preservedNames = null )
	{
		_preservedNames = preservedNames ?? new HashSet<string>();
	}

	private string GenerateName()
	{
		// Generate names like: _a, _b, ... _z, _aa, _ab, etc.
		var result = new StringBuilder( "_" );
		var num = _counter++;

		do
		{
			result.Append( (char)('a' + (num % 26)) );
			num = num / 26 - 1;
		}
		while ( num >= 0 );

		return result.ToString();
	}

	private string GetOrCreateName( string original )
	{
		// Don't rename preserved names or reserved C# names
		if ( _preservedNames.Contains( original ) || ReservedNames.Contains( original ) )
			return original;

		// Don't rename names starting with underscore that are already short
		if ( original.StartsWith( "_" ) && original.Length <= 3 )
			return original;

		if ( !_nameMap.TryGetValue( original, out var newName ) )
		{
			newName = GenerateName();
			_nameMap[original] = newName;
		}
		return newName;
	}

	/// <summary>
	/// Check if a member has private accessibility (no public/protected/internal modifiers).
	/// </summary>
	private bool IsPrivateMember( SyntaxTokenList modifiers )
	{
		if ( modifiers.Any( SyntaxKind.PublicKeyword ) ) return false;
		if ( modifiers.Any( SyntaxKind.ProtectedKeyword ) ) return false;
		if ( modifiers.Any( SyntaxKind.InternalKeyword ) ) return false;
		return true;
	}

	/// <summary>
	/// Check if a method can be renamed (private and not override/virtual/interface impl).
	/// </summary>
	private bool CanRenameMethod( SyntaxTokenList modifiers )
	{
		if ( !IsPrivateMember( modifiers ) ) return false;
		if ( modifiers.Any( SyntaxKind.OverrideKeyword ) ) return false;
		if ( modifiers.Any( SyntaxKind.VirtualKeyword ) ) return false;
		if ( modifiers.Any( SyntaxKind.AbstractKeyword ) ) return false;
		return true;
	}

	// ===== FIELDS =====
	public override SyntaxNode VisitFieldDeclaration( FieldDeclarationSyntax node )
	{
		if ( !IsPrivateMember( node.Modifiers ) )
			return base.VisitFieldDeclaration( node );

		var variables = node.Declaration.Variables;
		var newVariables = new List<VariableDeclaratorSyntax>();

		foreach ( var variable in variables )
		{
			var newName = GetOrCreateName( variable.Identifier.Text );
			var newIdentifier = SyntaxFactory.Identifier( newName )
				.WithTriviaFrom( variable.Identifier );
			newVariables.Add( variable.WithIdentifier( newIdentifier ) );
		}

		var newDeclaration = node.Declaration.WithVariables(
			SyntaxFactory.SeparatedList( newVariables ) );

		return base.VisitFieldDeclaration( node.WithDeclaration( newDeclaration ) );
	}

	// ===== PROPERTIES =====
	public override SyntaxNode VisitPropertyDeclaration( PropertyDeclarationSyntax node )
	{
		if ( !IsPrivateMember( node.Modifiers ) )
			return base.VisitPropertyDeclaration( node );

		// Don't rename if it's an interface implementation (has explicit interface specifier)
		if ( node.ExplicitInterfaceSpecifier != null )
			return base.VisitPropertyDeclaration( node );

		var newName = GetOrCreateName( node.Identifier.Text );
		var newIdentifier = SyntaxFactory.Identifier( newName )
			.WithTriviaFrom( node.Identifier );

		return base.VisitPropertyDeclaration( node.WithIdentifier( newIdentifier ) );
	}

	// ===== METHODS =====
	public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
	{
		if ( !CanRenameMethod( node.Modifiers ) )
			return base.VisitMethodDeclaration( node );

		// Don't rename if it's an interface implementation
		if ( node.ExplicitInterfaceSpecifier != null )
			return base.VisitMethodDeclaration( node );

		var newName = GetOrCreateName( node.Identifier.Text );
		var newIdentifier = SyntaxFactory.Identifier( newName )
			.WithTriviaFrom( node.Identifier );

		return base.VisitMethodDeclaration( node.WithIdentifier( newIdentifier ) );
	}

	// ===== EVENTS =====
	public override SyntaxNode VisitEventDeclaration( EventDeclarationSyntax node )
	{
		if ( !IsPrivateMember( node.Modifiers ) )
			return base.VisitEventDeclaration( node );

		var newName = GetOrCreateName( node.Identifier.Text );
		var newIdentifier = SyntaxFactory.Identifier( newName )
			.WithTriviaFrom( node.Identifier );

		return base.VisitEventDeclaration( node.WithIdentifier( newIdentifier ) );
	}

	public override SyntaxNode VisitEventFieldDeclaration( EventFieldDeclarationSyntax node )
	{
		if ( !IsPrivateMember( node.Modifiers ) )
			return base.VisitEventFieldDeclaration( node );

		var variables = node.Declaration.Variables;
		var newVariables = new List<VariableDeclaratorSyntax>();

		foreach ( var variable in variables )
		{
			var newName = GetOrCreateName( variable.Identifier.Text );
			var newIdentifier = SyntaxFactory.Identifier( newName )
				.WithTriviaFrom( variable.Identifier );
			newVariables.Add( variable.WithIdentifier( newIdentifier ) );
		}

		var newDeclaration = node.Declaration.WithVariables(
			SyntaxFactory.SeparatedList( newVariables ) );

		return base.VisitEventFieldDeclaration( node.WithDeclaration( newDeclaration ) );
	}

	// ===== DELEGATES =====
	public override SyntaxNode VisitDelegateDeclaration( DelegateDeclarationSyntax node )
	{
		if ( !IsPrivateMember( node.Modifiers ) )
			return base.VisitDelegateDeclaration( node );

		var newName = GetOrCreateName( node.Identifier.Text );
		var newIdentifier = SyntaxFactory.Identifier( newName )
			.WithTriviaFrom( node.Identifier );

		return base.VisitDelegateDeclaration( node.WithIdentifier( newIdentifier ) );
	}

	// ===== NESTED TYPES =====
	public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node )
	{
		// Only rename nested private classes
		if ( node.Parent is not TypeDeclarationSyntax )
			return base.VisitClassDeclaration( node );

		if ( !IsPrivateMember( node.Modifiers ) )
			return base.VisitClassDeclaration( node );

		var newName = GetOrCreateName( node.Identifier.Text );
		var newIdentifier = SyntaxFactory.Identifier( newName )
			.WithTriviaFrom( node.Identifier );

		return base.VisitClassDeclaration( node.WithIdentifier( newIdentifier ) );
	}

	public override SyntaxNode VisitStructDeclaration( StructDeclarationSyntax node )
	{
		// Only rename nested private structs
		if ( node.Parent is not TypeDeclarationSyntax )
			return base.VisitStructDeclaration( node );

		if ( !IsPrivateMember( node.Modifiers ) )
			return base.VisitStructDeclaration( node );

		var newName = GetOrCreateName( node.Identifier.Text );
		var newIdentifier = SyntaxFactory.Identifier( newName )
			.WithTriviaFrom( node.Identifier );

		return base.VisitStructDeclaration( node.WithIdentifier( newIdentifier ) );
	}

	public override SyntaxNode VisitEnumDeclaration( EnumDeclarationSyntax node )
	{
		// Only rename nested private enums
		if ( node.Parent is not TypeDeclarationSyntax )
			return base.VisitEnumDeclaration( node );

		if ( !IsPrivateMember( node.Modifiers ) )
			return base.VisitEnumDeclaration( node );

		var newName = GetOrCreateName( node.Identifier.Text );
		var newIdentifier = SyntaxFactory.Identifier( newName )
			.WithTriviaFrom( node.Identifier );

		return base.VisitEnumDeclaration( node.WithIdentifier( newIdentifier ) );
	}

	// ===== LOCAL VARIABLES =====
	public override SyntaxNode VisitVariableDeclarator( VariableDeclaratorSyntax node )
	{
		// Check if this is a local variable (inside a method/property)
		var parent = node.Parent?.Parent;
		if ( parent is LocalDeclarationStatementSyntax )
		{
			var newName = GetOrCreateName( node.Identifier.Text );
			var newIdentifier = SyntaxFactory.Identifier( newName )
				.WithTriviaFrom( node.Identifier );
			return base.VisitVariableDeclarator( node.WithIdentifier( newIdentifier ) );
		}

		return base.VisitVariableDeclarator( node );
	}

	// ===== PARAMETERS =====
	public override SyntaxNode VisitParameter( ParameterSyntax node )
	{
		var newName = GetOrCreateName( node.Identifier.Text );
		var newIdentifier = SyntaxFactory.Identifier( newName )
			.WithTriviaFrom( node.Identifier );
		return base.VisitParameter( node.WithIdentifier( newIdentifier ) );
	}

	// ===== IDENTIFIER REFERENCES =====
	public override SyntaxNode VisitIdentifierName( IdentifierNameSyntax node )
	{
		// Look up if this identifier was renamed
		if ( _nameMap.TryGetValue( node.Identifier.Text, out var newName ) )
		{
			var newIdentifier = SyntaxFactory.Identifier( newName )
				.WithTriviaFrom( node.Identifier );
			return node.WithIdentifier( newIdentifier );
		}

		return base.VisitIdentifierName( node );
	}

	/// <summary>
	/// Process a syntax tree, renaming private symbols.
	/// </summary>
	public static SyntaxTree Process( SyntaxTree tree, HashSet<string> preservedNames = null )
	{
		var rewriter = new SymbolRenamer( preservedNames );
		var newRoot = rewriter.Visit( tree.GetRoot() );
		return tree.WithRootAndOptions( newRoot, tree.Options );
	}

	/// <summary>
	/// Process multiple syntax trees with shared name mapping.
	/// This ensures consistent renaming across files.
	/// </summary>
	public static List<SyntaxTree> ProcessAll( IEnumerable<SyntaxTree> trees, HashSet<string> preservedNames = null )
	{
		var rewriter = new SymbolRenamer( preservedNames );
		var result = new List<SyntaxTree>();

		// First pass: collect all declarations to build the name map
		foreach ( var tree in trees )
		{
			rewriter.Visit( tree.GetRoot() );
		}

		// Second pass: apply the transformations
		rewriter._counter = 0;
		rewriter._nameMap.Clear();

		foreach ( var tree in trees )
		{
			var newRoot = rewriter.Visit( tree.GetRoot() );
			result.Add( tree.WithRootAndOptions( newRoot, tree.Options ) );
		}

		return result;
	}
}
