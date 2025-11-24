using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Sandbox.Generator
{
	internal class CodeGen
	{
		[Flags]
		internal enum Flags
		{
			WrapProperyGet = 1,
			WrapPropertySet = 2,
			WrapMethod = 4,
			Static = 8,
			Instance = 16
		}

		/// <summary>
		/// Find anything marked with [CodeGen] and perform the appropriate code generation.
		/// </summary>
		internal static void VisitMethod( ref MethodDeclarationSyntax node, IMethodSymbol symbol, Worker master )
		{
			// This will be true for abstract methods..
			if ( (node.Body == null && node.ExpressionBody == null) || symbol.IsAbstract ) return;

			bool hasTarget = false;
			var attributesToWrite = new List<string>();
			var attributes = symbol.GetAttributes();

			foreach ( var attribute in attributes )
			{
				foreach ( var cg in GetCodeGeneratorAttributes( attribute ) )
				{
					var type = (Flags)int.Parse( cg.GetArgumentValue( 0, "Type", "0" ) );
					var callbackName = cg.GetArgumentValue( 1, "CallbackName", string.Empty );
					if ( !type.Contains( Flags.WrapMethod ) ) continue;

					hasTarget = HandleWrapCall( attribute, type, callbackName, ref node, symbol, master ) || hasTarget;
				}

				// include ALL the attributes when passing to the thing
				AddAttributeString( attribute, attributesToWrite );
			}

			if ( hasTarget && attributesToWrite.Count > 0 )
			{
				var methodIdentity = MakeMethodIdentitySafe( GetUniqueMethodIdentity( symbol ) );
				master.AddToCurrentClass( $"[global::Sandbox.SkipHotload] static readonly global::System.Attribute[] __{methodIdentity}__Attrs = new global::System.Attribute[] {{ {string.Join( ", ", attributesToWrite )} }};\n", false );
			}
		}

		private struct PropertyWrapperData
		{
			public AttributeData Attribute { get; set; }
			public string CallbackName { get; set; }
			public int Priority { get; set; }
			public Flags Type { get; set; }
		}

		internal static void VisitProperty( ref PropertyDeclarationSyntax node, IPropertySymbol symbol, Worker master )
		{
			var generateBackingField = false;
			var attributesToWrite = new List<string>();
			var attributes = symbol.GetAttributes();
			var originalNode = node;
			var data = new List<PropertyWrapperData>();

			foreach ( var attribute in attributes )
			{
				foreach ( var cg in GetCodeGeneratorAttributes( attribute ) )
				{
					var type = (Flags)int.Parse( cg.GetArgumentValue( 0, "Type", "0" ) );
					var callbackName = cg.GetArgumentValue( 1, "CallbackName", string.Empty );
					var priority = int.Parse( cg.GetArgumentValue( 2, "Priority", "0" ) );

					if ( type.Contains( Flags.WrapPropertySet ) || type.Contains( Flags.WrapProperyGet ) )
					{
						data.Add( new()
						{
							Attribute = attribute,
							CallbackName = callbackName,
							Priority = priority,
							Type = type
						} );
					}

					AddAttributeString( attribute, attributesToWrite );
				}
			}

			data.Sort( ( a, b ) => b.Priority.CompareTo( a.Priority ) );

			foreach ( var w in data )
			{
				if ( w.Type.Contains( Flags.WrapPropertySet ) )
				{
					if ( HandleWrapSet( w.Attribute, w.Type, w.CallbackName, ref node, symbol, master ) )
						generateBackingField = true;
				}

				if ( w.Type.Contains( Flags.WrapProperyGet ) )
				{
					if ( HandleWrapGet( w.Attribute, w.Type, w.CallbackName, ref node, symbol, master ) )
						generateBackingField = true;
				}
			}

			if ( attributesToWrite.Count > 0 )
			{
				master.AddToCurrentClass( $"[global::Sandbox.SkipHotload] static readonly global::System.Attribute[] __{symbol.Name}__Attrs = new global::System.Attribute[] {{ {string.Join( ", ", attributesToWrite )} }};\n", false );
			}

			if ( !generateBackingField ) return;

			var fieldName = originalNode.BackingFieldName();
			var modifiers = originalNode.Modifiers.ToString();
			var nodeType = originalNode.Type;

			modifiers = modifiers.Replace( "public", "" ).Replace( "protected", "" ).Trim();
			master.AddToCurrentClass( $"{modifiers} {nodeType} {fieldName}{originalNode.Initializer};\n", false );
		}

		private static void AddAttributeString( AttributeData attribute, List<string> list )
		{
			var sn = attribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
			if ( sn is null ) return;

			var attributeClassName = attribute.AttributeClass.FullName();
			var propertyArguments = new List<(string, string)>();
			var regularArguments = new List<string>();

			if ( !attributeClassName.EndsWith( "Attribute" ) )
				attributeClassName += "Attribute";

			var arguments = sn.ArgumentList?.Arguments.ToArray() ?? Array.Empty<AttributeArgumentSyntax>();
			if ( arguments.Length == 0 )
			{
				list.Add( $"new {attributeClassName}()" );
				return;
			}

			foreach ( var syntax in arguments )
			{
				if ( syntax.NameColon is not null )
					propertyArguments.Add( (syntax.NameColon.Name.ToString(), syntax.Expression.ToString()) );
				else if ( syntax.NameEquals != null )
					propertyArguments.Add( (syntax.NameEquals.Name.ToString(), syntax.Expression.ToString()) );
				else
					regularArguments.Add( syntax.Expression.ToString() );
			}

			var output = $"new {attributeClassName}( {string.Join( ",", regularArguments )} ) {{ ";

			for ( var i = 0; i < propertyArguments.Count; i++ )
			{
				var (k, v) = propertyArguments[i];
				output += $"{k} = {v}";

				if ( i < propertyArguments.Count - 1 )
				{
					output += ", ";
				}
			}

			list.Add( $"{output} }}" );
		}

		private static bool HandleWrapSet( AttributeData attribute, Flags type, string callbackName, ref PropertyDeclarationSyntax node, IPropertySymbol symbol, Worker master )
		{
			if ( symbol.IsStatic && !type.Contains( Flags.Static ) )
				return false;

			if ( !symbol.IsStatic && !type.Contains( Flags.Instance ) )
				return false;

			var typeToInvokeOn = symbol.ContainingType;
			var methodToInvoke = callbackName;
			var splitCallbackName = callbackName.Split( '.' );
			var isStaticCallback = false;

			if ( splitCallbackName.Length > 1 )
			{
				isStaticCallback = true;
				methodToInvoke = splitCallbackName[splitCallbackName.Length - 1];

				var typeToLookFor = string.Join( ".", splitCallbackName.Take( splitCallbackName.Length - 1 ) );
				typeToInvokeOn = master.GetOrCreateTypeByMetadataName( typeToLookFor );

				if ( typeToInvokeOn is null )
				{
					master.AddError( node.GetLocation(),
						$"Unable to find {typeToLookFor} required for {attribute.AttributeClass?.Name}. Ensure that a fully qualified callback name is used." );
					return false;
				}
			}

			if ( typeToInvokeOn is null || !ValidateSetterCallback( symbol.ContainingType, typeToInvokeOn, methodToInvoke, isStaticCallback, symbol.Type ) )
			{
				master.AddError( node.GetLocation(),
					$"A method {callbackName}( WrappedPropertySet ) is required on {typeToInvokeOn?.Name}." );

				return false;
			}

			var propertyType = symbol.Type.FullName();
			var usesBackingField = false;
			var accessors = new List<AccessorDeclarationSyntax>();
			var get = node.AccessorList?.Accessors.FirstOrDefault( a => a.Kind() == SyntaxKind.GetAccessorDeclaration );

			if ( get is not null && (get.Body is null && get.ExpressionBody is null) )
			{
				var defaultStatement = ParseStatement( $"return {node.BackingFieldName()};" );
				usesBackingField = true;
				get = AccessorDeclaration( SyntaxKind.GetAccessorDeclaration, Block( defaultStatement ) );
			}

			if ( get is not null )
			{
				accessors.Add( get );
			}

			{
				var existingSetter = node.AccessorList?.Accessors.FirstOrDefault( a => a.Kind() == SyntaxKind.SetAccessorDeclaration );
				string defaultStatement = $"{node.BackingFieldName()} = value;";
				string getValue = $"{node.BackingFieldName()}";

				if ( existingSetter is not null && (existingSetter.Body is not null || existingSetter.ExpressionBody is not null) )
				{
					if ( existingSetter.Body is not null )
						defaultStatement = existingSetter.Body.ToString();
					else
						defaultStatement = $"{existingSetter.ExpressionBody?.Expression.ToString()};";
				}
				else
				{
					usesBackingField = true;
				}

				var memberIdentity = $"{symbol.ContainingType.GetFullMetadataName().Replace( "global::", "" )}.{symbol.Name}";

				var parameterStruct = ParseStatement(
					$"new global::Sandbox.WrappedPropertySet<{propertyType}> {{" +
					$"Value = value," +
					$"Object = {(symbol.IsStatic ? "null" : "this")}," +
					$"Setter = ( v ) => {{{defaultStatement}}}," +
					$"Getter = () => {symbol.Name}," +
					$"IsStatic = {(symbol.IsStatic ? "true" : "false")}," +
					$"TypeName = {symbol.ContainingType.FullName().Replace( "global::", "" ).QuoteSafe()}," +
					$"PropertyName = {symbol.Name.QuoteSafe()}," +
					$"MemberIdent = {memberIdentity.FastHash()}," +
					$"Attributes = __{symbol.Name}__Attrs" +
					$"}}" );

				var statements = new[]
				{
					ParseStatement( $"{callbackName}( {parameterStruct} );" )
				};

				var set = AccessorDeclaration( SyntaxKind.SetAccessorDeclaration, Block( statements ) );

				if ( existingSetter is not null )
					set = set.WithModifiers( existingSetter.Modifiers );

				accessors.Add( set );

				node = node.WithAccessorList( AccessorList( List( accessors ) ) )
					.WithInitializer( null )
					.WithSemicolonToken( Token( SyntaxKind.None ) )
					.NormalizeWhitespace();
			}

			return usesBackingField;
		}

		private static bool HandleWrapGet( AttributeData attribute, Flags type, string callbackName, ref PropertyDeclarationSyntax node, IPropertySymbol symbol, Worker master )
		{
			if ( symbol.IsStatic && !type.Contains( Flags.Static ) )
				return false;

			if ( !symbol.IsStatic && !type.Contains( Flags.Instance ) )
				return false;

			var typeToInvokeOn = symbol.ContainingType;
			var methodToInvoke = callbackName;
			var splitCallbackName = callbackName.Split( '.' );
			var isStaticCallback = false;

			if ( splitCallbackName.Length > 1 )
			{
				isStaticCallback = true;
				methodToInvoke = splitCallbackName[splitCallbackName.Length - 1];

				var typeToLookFor = string.Join( ".", splitCallbackName.Take( splitCallbackName.Length - 1 ) );
				typeToInvokeOn = master.GetOrCreateTypeByMetadataName( typeToLookFor );

				if ( typeToInvokeOn is null )
				{
					master.AddError( node.GetLocation(),
						$"Unable to find {typeToLookFor} required for {attribute.AttributeClass?.Name}. Ensure that a fully qualified callback name is used." );
					return false;
				}
			}

			var propertyType = symbol.Type.FullName();

			if ( typeToInvokeOn is null || !ValidateGetterCallback( symbol.ContainingType, typeToInvokeOn, methodToInvoke, isStaticCallback, symbol.Type ) )
			{
				master.AddError( node.GetLocation(),
					$"A method {symbol.Type.Name} {methodToInvoke}( WrappedPropertyGet ) is required on {typeToInvokeOn?.Name}." );

				return false;
			}

			var usesBackingField = false;
			var accessors = new List<AccessorDeclarationSyntax>();
			var set = node.AccessorList?.Accessors.FirstOrDefault( a => a.Kind() == SyntaxKind.SetAccessorDeclaration );

			if ( set is not null && (set.Body is null && set.ExpressionBody is null) )
			{
				var defaultStatement = ParseStatement( $"{node.BackingFieldName()} = value;" );
				usesBackingField = true;
				set = AccessorDeclaration( SyntaxKind.SetAccessorDeclaration, Block( defaultStatement ) );
			}

			if ( set is not null )
			{
				accessors.Add( set );
			}

			{
				var existingGetter = node.AccessorList?.Accessors.FirstOrDefault( a => a.Kind() == SyntaxKind.GetAccessorDeclaration );
				var defaultStatement = string.Empty;
				var defaultValue = $"value";

				if ( existingGetter is not null && (existingGetter.Body is not null || existingGetter.ExpressionBody is not null) )
				{
					if ( existingGetter.Body is not null )
					{
						defaultStatement = $"var getValue = () => {existingGetter.Body.ToString()};";
						defaultValue = $"getValue()";
					}
					else
					{
						defaultValue = $"{existingGetter.ExpressionBody?.Expression.ToString()}";
					}
				}
				else
				{
					defaultStatement = $"var value = {node.BackingFieldName()};";
					usesBackingField = true;
				}

				var statements = new List<StatementSyntax>();

				if ( !string.IsNullOrEmpty( defaultStatement ) )
				{
					statements.Add( ParseStatement( defaultStatement ) );
				}

				var memberIdentity = $"{symbol.ContainingType.GetFullMetadataName().Replace( "global::", "" )}.{symbol.Name}";
				var parameterStruct = ParseStatement(
					$"new global::Sandbox.WrappedPropertyGet<{propertyType}> {{" +
					$"Value = {defaultValue}," +
					$"Object = {(symbol.IsStatic ? "null" : "this")}," +
					$"IsStatic = {(symbol.IsStatic ? "true" : "false")}," +
					$"TypeName = {symbol.ContainingType.FullName().Replace( "global::", "" ).QuoteSafe()}," +
					$"PropertyName = {symbol.Name.QuoteSafe()}," +
					$"MemberIdent = {memberIdentity.FastHash()}," +
					$"Attributes = __{symbol.Name}__Attrs" +
					$"}}" );

				statements.Add(
					ParseStatement( $"return ({propertyType}){callbackName}( {parameterStruct} );" ) );

				var get = AccessorDeclaration( SyntaxKind.GetAccessorDeclaration, Block( statements ) );

				if ( existingGetter is not null )
					get = get.WithModifiers( existingGetter.Modifiers );

				accessors.Add( get );

				node = node.WithAccessorList( AccessorList( List( accessors ) ) )
					.WithInitializer( null )
					.WithSemicolonToken( Token( SyntaxKind.None ) )
					.NormalizeWhitespace();
			}

			return usesBackingField;
		}

		private static readonly Dictionary<string, string> TypeAliases = new()
		{
			["object"] = "System.Object",
			["string"] = "System.String",
			["bool"] = "System.Boolean",
			["byte"] = "System.Byte",
			["sbyte"] = "System.SByte",
			["short"] = "System.Int16",
			["ushort"] = "System.UInt16",
			["int"] = "System.Int32",
			["uint"] = "System.UInt32",
			["long"] = "System.Int64",
			["ulong"] = "System.UInt64",
			["float"] = "System.Single",
			["double"] = "System.Double",
			["decimal"] = "System.Decimal",
			["char"] = "System.Char"
		};

		private static string SanitizeTypeName( ITypeSymbol type, bool fullName = false )
		{
			if ( type is IArrayTypeSymbol a ) return $"{SanitizeTypeName( a.ElementType )}[]";

			if ( !fullName )
			{
				return TypeAliases.TryGetValue( type.Name, out var alias ) ? alias : type.Name;
			}

			return type.FullName()
				.Replace( "global::", "" )
				.Split( '<' )
				.FirstOrDefault();
		}

		private static bool HandleWrapCall( AttributeData attribute, Flags type, string callbackName, ref MethodDeclarationSyntax node, IMethodSymbol symbol, Worker master )
		{
			if ( node.Body == null && node.ExpressionBody == null ) return false;

			var parameterCount = symbol.Parameters.Count();
			var parameterList = string.Join( ", ", symbol.Parameters.Select( s => s.Name ) );

			if ( symbol.IsStatic && !type.Contains( Flags.Static ) )
				return false;

			if ( !symbol.IsStatic && !type.Contains( Flags.Instance ) )
				return false;

			var typeToInvokeOn = symbol.ContainingType;
			var methodToInvoke = callbackName;
			var splitCallbackName = callbackName.Split( '.' );
			var isStaticCallback = false;

			if ( splitCallbackName.Length > 1 )
			{
				isStaticCallback = true;
				methodToInvoke = splitCallbackName[splitCallbackName.Length - 1];

				var typeToLookFor = string.Join( ".", splitCallbackName.Take( splitCallbackName.Length - 1 ) );
				typeToInvokeOn = master.GetOrCreateTypeByMetadataName( typeToLookFor );

				if ( typeToInvokeOn is null )
				{
					master.AddError( node.GetLocation(),
						$"Unable to find {typeToLookFor} required for {attribute.AttributeClass?.Name}. Ensure that a fully qualified callback name is used." );
					return false;
				}
			}

			var success = false;

			if ( typeToInvokeOn is not null )
			{
				success = ValidateMethodCallback( symbol.ContainingType, typeToInvokeOn, methodToInvoke,
					isStaticCallback, !symbol.ReturnsVoid ? symbol.ReturnType : null, parameterCount );
			}

			if ( !success )
			{
				var returnType = symbol.ReturnsVoid ? string.Empty : $"{symbol.ReturnType.Name} ";
				var paramsString = string.Join( ", ", Enumerable.Repeat( "Object", parameterCount ) );

				if ( symbol.ReturnsVoid )
				{
					master.AddError( node.GetLocation(),
						parameterCount > 0
							? $"A method {returnType}{methodToInvoke}( WrappedMethod, {paramsString} ) is required on {typeToInvokeOn?.Name}."
							: $"A method {returnType}{methodToInvoke}( WrappedMethod ) is required on {typeToInvokeOn?.Name}." );
				}
				else
				{
					master.AddError( node.GetLocation(),
						parameterCount > 0
							? $"A method {returnType}{methodToInvoke}( WrappedMethod<{symbol.ReturnType.Name}>, {paramsString} ) is required on {typeToInvokeOn?.Name}."
							: $"A method {returnType}{methodToInvoke}( WrappedMethod<{symbol.ReturnType.Name}> ) is required on {typeToInvokeOn?.Name}." );
				}

				return false;
			}

			var parameterStructGenericType = string.Empty;
			if ( !symbol.ReturnsVoid )
				parameterStructGenericType = $"<{symbol.ReturnType.FullName()}>";

			var resumeString = "{}";

			if ( node.Body is not null )
			{
				if ( node.Body.Statements.Any() )
					resumeString = $"{node.Body.ToFullString()}";
			}
			else if ( node.ExpressionBody is not null )
				resumeString = node.ExpressionBody.Expression.ToFullString();

			string resumeExpression;

			if ( symbol.IsAsync )
				resumeExpression = $"async () => {resumeString}";
			else
				resumeExpression = $"() => {resumeString}";

			var methodIdentity = GetUniqueMethodIdentity( symbol );
			var parameterStruct = ParseStatement(
				$"new global::Sandbox.WrappedMethod{parameterStructGenericType} {{" +
				$"Resume = {resumeExpression}," +
				$"Object = {(symbol.IsStatic ? "null" : "this")}," +
				$"MethodIdentity = {methodIdentity}," +
				$"MethodName = {symbol.Name.QuoteSafe()}," +
				$"TypeName = {symbol.ContainingType.FullName().Replace( "global::", "" ).QuoteSafe()}," +
				$"IsStatic = {(symbol.IsStatic ? "true" : "false")}," +
				$"Attributes = __{MakeMethodIdentitySafe( methodIdentity )}__Attrs" +
				$"}}" );

			var fullReturnType = symbol.ReturnType.FullName();
			var isGenericTaskType = fullReturnType.StartsWith( "global::System.Threading.Tasks.Task<" );
			var isTaskType = fullReturnType == "global::System.Threading.Tasks.Task";

			var callbackCall = parameterCount > 0
				? $"{callbackName}( {parameterStruct}, {parameterList} )"
				: $"{callbackName}( {parameterStruct} )";

			if ( node.ExpressionBody is null )
			{
				List<StatementSyntax> statements;

				if ( symbol.IsAsync )
				{
					if ( isGenericTaskType )
					{
						statements = new List<StatementSyntax>
						{
							ParseStatement( $"return await {callbackCall};" )
						};
					}
					else if ( isTaskType )
					{
						statements = new List<StatementSyntax>
						{
							ParseStatement( $"await {callbackCall};" ),
							ParseStatement( "return;" )
						};
					}
					else if ( symbol.ReturnsVoid )
					{
						statements = new List<StatementSyntax>
						{
							ParseStatement( $"{callbackCall};" )
						};
					}
					else
					{
						statements = new List<StatementSyntax>
						{
							ParseStatement( $"return {callbackCall};" )
						};
					}
				}
				else
				{
					var returnPrefix = symbol.ReturnsVoid ? "" : "return ";

					statements = new List<StatementSyntax>
					{
						ParseStatement( $"{returnPrefix}{callbackCall};" )
					};
				}

				var block = Block( statements );

				var newBody = block.WithCloseBraceToken(
					block.CloseBraceToken.WithTrailingTrivia( SyntaxTriviaList.Empty )
				);

				node = node.WithBody( newBody );
			}
			else
			{
				if ( symbol.IsAsync && isTaskType )
				{
					var statements = new[]
					{
						ParseStatement( $"await {callbackCall};" ),
						ParseStatement( "return;" )
					};

					node = node
						.WithExpressionBody( null )
						.WithSemicolonToken( Token( SyntaxKind.None ) )
						.WithBody( Block( statements ) );
				}
				else
				{
					var expression = (symbol.IsAsync && isGenericTaskType)
						? $"await {callbackCall}"
						: callbackCall;

					node = node.WithExpressionBody(
						node.ExpressionBody.WithExpression( ParseExpression( expression ) )
					);
				}
			}

			return true;
		}

		private static string GetUniqueMethodIdentityString( IMethodSymbol method )
		{
			// Needs to keep in sync with Sandbox.MethodDescription.GetIdentityHashString()

			// TODO: this will have conflicts for generic types with different numbers of type params

			var returnTypeName = method.ReturnsVoid ? "Void" : SanitizeTypeName( method.ReturnType );
			return $"{returnTypeName}.{SanitizeTypeName( method.ContainingType, true )}.{method.Name}.{string.Join( ",", method.Parameters.Select( p => SanitizeTypeName( p.Type ) ) )}";
		}

		private static int GetUniqueMethodIdentity( IMethodSymbol method )
		{
			return GetUniqueMethodIdentityString( method ).FastHash();
		}

		private static string MakeMethodIdentitySafe( int identity )
		{
			return identity.ToString().Replace( "-", "m_" );
		}

		private static IEnumerable<IMethodSymbol> FetchValidMethods( INamedTypeSymbol parent, string methodName, bool isStatic = false, bool isRootType = false )
		{
			var validMethods = parent.GetMembers().OfType<IMethodSymbol>()
				.Where( s => (!isStatic || s.IsStatic) && s.Name == methodName )
				.Where( s => s.DeclaredAccessibility != Accessibility.Private || isRootType );

			foreach ( var symbol in validMethods )
			{
				yield return symbol;
			}

			// If our target method is static we shouldn't look at base types.
			if ( isStatic )
				yield break;

			if ( parent.BaseType is null )
				yield break;

			foreach ( var symbol in FetchValidMethods( parent.BaseType, methodName ) )
			{
				yield return symbol;
			}
		}

		private static bool ValidateMethodCallback( INamedTypeSymbol containingType, INamedTypeSymbol parent, string methodName, bool isStatic, ITypeSymbol returnType, int argCount )
		{
			var validMethods = FetchValidMethods( parent, methodName, isStatic, SymbolEqualityComparer.Default.Equals( containingType, parent ) );

			foreach ( var method in validMethods )
			{
				var hasObjectParams = method.Parameters.Length > 1 && method.Parameters[1].IsParams && method.Parameters[1].Type.FullName() == "object[]";

				if ( !hasObjectParams && method.Parameters.Length != argCount + 1 )
					continue;

				var firstParameterType = method.Parameters[0].Type;
				var firstParameterName = firstParameterType.FullName();

				if ( returnType is null )
				{
					if ( firstParameterName != "global::Sandbox.WrappedMethod" )
						continue;
				}
				else
				{
					if ( !firstParameterName.StartsWith( "global::Sandbox.WrappedMethod<" ) )
						continue;

					var namedParam = firstParameterType as INamedTypeSymbol;
					var wrappedArg = namedParam?.TypeArguments[0];

					if ( wrappedArg is null )
						continue;

					if ( !SymbolEqualityComparer.Default.Equals( wrappedArg, returnType )
						 && !IsTypeCompatible( wrappedArg, returnType ) )
					{
						continue;
					}

					var cbReturn = method.ReturnType;

					if ( !SymbolEqualityComparer.Default.Equals( cbReturn, returnType )
						 && !IsTypeCompatible( cbReturn, returnType )
						 && cbReturn is not ITypeParameterSymbol )
					{
						continue;
					}
				}

				return true;
			}

			return false;
		}

		private static bool IsTypeCompatible( ITypeSymbol candidate, ITypeSymbol target )
		{
			if ( candidate is ITypeParameterSymbol )
				return true;

			if ( candidate is not INamedTypeSymbol namedCandidate || target is not INamedTypeSymbol namedTarget )
				return false;

			if ( !SymbolEqualityComparer.Default.Equals( namedCandidate.OriginalDefinition, namedTarget.OriginalDefinition ) )
				return false;

			var candidateArgs = namedCandidate.TypeArguments;
			var targetArgs = namedTarget.TypeArguments;

			if ( candidateArgs.Length != targetArgs.Length )
				return false;

			for ( var i = 0; i < candidateArgs.Length; i++ )
			{
				var candidateArg = candidateArgs[i];
				var targetArg = targetArgs[i];

				if ( SymbolEqualityComparer.Default.Equals( candidateArg, targetArg ) )
					continue;

				if ( candidateArg is ITypeParameterSymbol )
					continue;

				if ( candidateArg is not INamedTypeSymbol candidateNamedArg || targetArg is not INamedTypeSymbol targetNamedArg )
					return false;

				if ( !IsTypeCompatible( candidateNamedArg, targetNamedArg ) )
					return false;
			}

			return true;
		}

		private static bool ValidateSetterCallback( INamedTypeSymbol containingType, INamedTypeSymbol parent, string methodName, bool isStatic, ITypeSymbol propertyType )
		{
			var validMethods = FetchValidMethods( parent, methodName, isStatic, SymbolEqualityComparer.Default.Equals( containingType, parent ) );

			foreach ( var method in validMethods )
			{
				if ( method.Parameters.Count() != 1 )
					continue;

				if ( !method.Parameters[0].Type.FullName().StartsWith( "global::Sandbox.WrappedPropertySet<" ) )
					continue;

				var namedParameterType = method.Parameters[0].Type as INamedTypeSymbol;
				if ( !SymbolEqualityComparer.Default.Equals( namedParameterType?.TypeArguments[0], propertyType )
					 && namedParameterType?.TypeArguments[0] is not ITypeParameterSymbol )
					continue;

				return true;
			}

			return false;
		}

		private static bool ValidateGetterCallback( INamedTypeSymbol containingType, INamedTypeSymbol parent, string methodName, bool isStatic, ITypeSymbol propertyType )
		{
			var validMethods = FetchValidMethods( parent, methodName, isStatic, SymbolEqualityComparer.Default.Equals( containingType, parent ) );

			foreach ( var method in validMethods )
			{
				if ( method.Parameters.Count() != 1 )
					continue;

				if ( !method.Parameters[0].Type.FullName().StartsWith( "global::Sandbox.WrappedPropertyGet<" ) )
					continue;

				var namedParameterType = method.Parameters[0].Type as INamedTypeSymbol;
				if ( !SymbolEqualityComparer.Default.Equals( namedParameterType?.TypeArguments[0], propertyType )
					 && namedParameterType?.TypeArguments[0] is not ITypeParameterSymbol )
					continue;


				return true;
			}

			return false;
		}

		private static bool IsCodeGeneratorAttribute( AttributeData attribute )
		{
			return attribute.AttributeClass.FullName() == "global::Sandbox.CodeGeneratorAttribute";
		}

		private static IEnumerable<AttributeData> GetCodeGeneratorAttributes( AttributeData parent )
		{
			return parent.AttributeClass?.GetAttributes().Where( IsCodeGeneratorAttribute );
		}
	}
}
