// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Rewrites calls to interfaces methods with calls to their native
    /// implemented ones.
    /// </summary>
    internal class NativeRegisterRewriter : CSharpSyntaxRewriter
    {
        // This class transforms an expression in the form of:
        //// interfaceInstance.Register<Class>(arguments);
        // to:
        //// ExportedNativeMethods.Register_Method1(arguments, "Class_Method1");
        //// ExportedNativeMethods.Register_Method2(arguments, "Class_Method2");
        private readonly TypeSyntax adapterType;
        private readonly ManagedCallbackGenerator callbackGenerator;
        private readonly string classToReplace;
        private readonly SemanticModel model;

        private readonly List<(SyntaxNode original, SyntaxNode[] replacements)> nodesToReplace =
            new List<(SyntaxNode, SyntaxNode[])>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeRegisterRewriter"/> class.
        /// </summary>
        /// <param name="model">Contains the semantic information.</param>
        /// <param name="callbackGenerator">
        /// Allows the creation of callback methods.
        /// </param>
        /// <param name="classType">
        /// The class type to replace the static calls from.
        /// </param>
        /// <param name="adapterType">
        /// The adapter type to replace the calls to.
        /// </param>
        public NativeRegisterRewriter(
            SemanticModel model,
            ManagedCallbackGenerator callbackGenerator,
            Type classType,
            Type adapterType)
        {
            this.model = model;
            this.callbackGenerator = callbackGenerator;
            this.adapterType = ParseTypeName(adapterType.FullName.Replace('+', '.'));
            this.classToReplace = classType.FullName.Replace('+', '.');
        }

        /// <inheritdoc />
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax member)
            {
                var method = (IMethodSymbol)this.model
                    .GetMemberGroup(member.Name)
                    .Single();

                string containingType = method.ContainingType.ToDisplayString();
                if (method.IsGenericMethod &&
                    this.classToReplace.Equals(containingType, StringComparison.Ordinal))
                {
                    SyntaxNode[] replacements =
                        this.ReplaceNode(node, method)
                        .Select(n => ExpressionStatement(n).WithTriviaFrom(node.Parent))
                        .ToArray();

                    this.nodesToReplace.Add((node.Parent, replacements));
                    this.ReplaceNode(node, method);
                }
            }

            return base.VisitInvocationExpression(node);
        }

        /// <inheritdoc />
        public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
        {
            SyntaxList<TNode> newList = base.VisitList(list);
            for (int i = this.nodesToReplace.Count - 1; i >= 0; i--)
            {
                (SyntaxNode original, SyntaxNode[] replacements) = this.nodesToReplace[i];
                if (original is TNode)
                {
                    newList = newList.ReplaceRange((TNode)original, replacements.Cast<TNode>());
                    this.nodesToReplace.RemoveAt(i);
                }
            }

            return newList;
        }

        private static IEnumerable<IMethodSymbol> GetMethodsToExport(IMethodSymbol methodInfo)
        {
            return methodInfo
                .OriginalDefinition
                .TypeParameters
                .SelectMany(t => t.ConstraintTypes)
                .SelectMany(c => c.GetMembers())
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary);
        }

        private IEnumerable<InvocationExpressionSyntax> ReplaceNode(
            InvocationExpressionSyntax invocation,
            IMethodSymbol originalMethod)
        {
            foreach (IMethodSymbol method in GetMethodsToExport(originalMethod))
            {
                ArgumentSyntax memberArgument = Argument(
                    LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Literal(this.callbackGenerator.CreateMethod(method).ToString())));

                ExpressionSyntax adapterMethod = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    this.adapterType,
                    IdentifierName(method.Name));

                ArgumentListSyntax arguments = invocation.ArgumentList
                    .AddArguments(memberArgument)
                    .NormalizeWhitespace();

                yield return InvocationExpression(adapterMethod)
                    .WithArgumentList(arguments)
                    .WithTriviaFrom(invocation);
            }
        }
    }
}
