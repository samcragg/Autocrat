// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Immutable;
    using Autocrat.Abstractions;
    using Autocrat.Compiler.CodeGeneration;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Rewrites usages of delegates marked as being exported to the native code.
    /// </summary>
    internal class NativeDelegateRewriter
    {
        // This class changes callback delegates into an index that can be used
        // to register a managed method that is called from the native code.
        // For example, given this:
        //
        //// NativeRegister(this.HandleMessage);
        //
        // Then the following needs to be generated (here 123 is the result
        // when registering the method as an exported one):
        //
        //// NativeRegister(123);
        //
        // The actual generation of that method is done by ManagedCallbackGenerator
        private readonly ManagedCallbackGenerator callbackGenerator;
        private readonly SemanticModel model;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeDelegateRewriter"/> class.
        /// </summary>
        /// <param name="callbackGenerator">Generates the managed stub methods.</param>
        /// <param name="model">Contains the semantic information.</param>
        public NativeDelegateRewriter(ManagedCallbackGenerator callbackGenerator, SemanticModel model)
        {
            this.callbackGenerator = callbackGenerator;
            this.model = model;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeDelegateRewriter"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected NativeDelegateRewriter()
        {
            this.callbackGenerator = null!;
            this.model = null!;
        }

        /// <summary>
        /// Transforms any argument that represents a native delegate.
        /// </summary>
        /// <param name="argument">The argument to transform.</param>
        /// <returns>
        /// The native method handle if the argument represents a native
        /// delegate; otherwise, the original argument.
        /// </returns>
        public virtual ArgumentSyntax TransformArgument(ArgumentSyntax argument)
        {
            TypeInfo typeInfo = this.model.GetTypeInfo(argument.Expression);
            string? signature;
            if ((typeInfo.ConvertedType is null) ||
                (typeInfo.ConvertedType.TypeKind != TypeKind.Delegate) ||
                ((signature = GetNativeSignature(typeInfo.ConvertedType)) == null))
            {
                return argument;
            }

            return argument.Expression.Kind() switch
            {
                SyntaxKind.IdentifierName =>
                    this.ExportIdentifer(signature, argument.Expression),

                SyntaxKind.SimpleMemberAccessExpression =>
                    this.ExportMethodAccess(signature, (MemberAccessExpressionSyntax)argument.Expression),

                _ => throw new InvalidOperationException("Unable to export expression"),
            };
        }

        private static string? GetNativeSignature(ITypeSymbol symbol)
        {
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (RoslynHelper.IsOfType<NativeDelegateAttribute>(attribute.AttributeClass) &&
                    (attribute.ConstructorArguments.Length == 1))
                {
                    return (string?)attribute.ConstructorArguments[0].Value;
                }
            }

            return null;
        }

        private ArgumentSyntax ExportIdentifer(string signature, ExpressionSyntax expression)
        {
            SymbolInfo symbolInfo = this.model.GetSymbolInfo(expression);
            if (symbolInfo.Symbol?.Kind != SymbolKind.Method)
            {
                throw new InvalidOperationException("Expected a method: " + expression.ToString());
            }

            return this.ExportMethod(signature, (IMethodSymbol)symbolInfo.Symbol);
        }

        private ArgumentSyntax ExportMethod(string signature, IMethodSymbol method)
        {
            int handle = this.callbackGenerator.CreateMethod(signature, method);
            return Argument(LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                Literal(handle)));
        }

        private ArgumentSyntax ExportMethodAccess(string signature, MemberAccessExpressionSyntax memberAccess)
        {
            TypeInfo typeInfo = this.model.GetTypeInfo(memberAccess.Expression);
            ImmutableArray<ISymbol> members = ImmutableArray<ISymbol>.Empty;
            if (!(typeInfo.Type is null))
            {
                members = typeInfo.Type.GetMembers(memberAccess.Name.ToString());
            }

            if (members.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected a single method to be found for " + memberAccess.Expression.ToString());
            }

            return this.ExportMethod(signature, (IMethodSymbol)members[0]);
        }
    }
}
