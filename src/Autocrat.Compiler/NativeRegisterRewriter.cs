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
        //// ExportedNativeMethods.Register_Method1(arguments, 0);
        //// ExportedNativeMethods.Register_Method2(arguments, 1);
        private readonly ManagedCallbackGenerator callbackGenerator;
        private readonly SemanticModel model;

        private readonly List<(SyntaxNode original, SyntaxNode[] replacements)> nodesToReplace =
            new List<(SyntaxNode, SyntaxNode[])>();

        private readonly SignatureGenerator signatureGenerator;

        private readonly Dictionary<string, TypeSyntax> typesToReplace =
            new Dictionary<string, TypeSyntax>(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeRegisterRewriter"/> class.
        /// </summary>
        /// <param name="model">Contains the semantic information.</param>
        /// <param name="callbackGenerator">
        /// Allows the creation of callback methods.
        /// </param>
        /// <param name="signatureGenerator">
        /// Generates the native method signatures.
        /// </param>
        public NativeRegisterRewriter(
            SemanticModel model,
            ManagedCallbackGenerator callbackGenerator,
            SignatureGenerator signatureGenerator)
        {
            this.model = model;
            this.callbackGenerator = callbackGenerator;
            this.signatureGenerator = signatureGenerator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeRegisterRewriter"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected NativeRegisterRewriter()
        {
        }

        /// <summary>
        /// Adds the specified class to the list of types that will be replaced.
        /// </summary>
        /// <param name="classType">
        /// The class type to replace the static calls from.
        /// </param>
        /// <param name="adapterType">
        /// The adapter type to replace the calls to.
        /// </param>
        public virtual void AddReplacement(ITypeSymbol classType, ITypeSymbol adapterType)
        {
            this.typesToReplace.Add(
                classType.ToDisplayString(),
                ParseTypeName(adapterType.ToDisplayString()));
        }

        /// <inheritdoc />
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if ((node.Expression is MemberAccessExpressionSyntax member) &&
                this.TryGetAdapterType(member, out TypeSyntax adapterType, out IMethodSymbol method))
            {
                SyntaxNode[] replacements =
                    this.ReplaceNode(node, method, adapterType)
                        .Select(n => ExpressionStatement(n).WithTriviaFrom(node.Parent))
                        .ToArray();

                this.nodesToReplace.Add((node.Parent, replacements));
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
            ITypeSymbol concreteType = methodInfo.TypeArguments.Single();
            return methodInfo
                .OriginalDefinition
                .TypeParameters
                .SelectMany(t => t.ConstraintTypes)
                .SelectMany(c => c.GetMembers())
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary)
                .Select(m => (IMethodSymbol)concreteType.FindImplementationForInterfaceMember(m));
        }

        private IEnumerable<InvocationExpressionSyntax> ReplaceNode(
            InvocationExpressionSyntax invocation,
            IMethodSymbol originalMethod,
            TypeSyntax adapterType)
        {
            foreach (IMethodSymbol method in GetMethodsToExport(originalMethod))
            {
                string signature = this.signatureGenerator.GetSignature(method);
                ArgumentSyntax memberArgument = Argument(
                    LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        Literal(this.callbackGenerator.CreateMethod(signature, method))));

                ExpressionSyntax adapterMethod = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    adapterType,
                    IdentifierName(method.Name));

                ArgumentListSyntax arguments = invocation.ArgumentList
                    .AddArguments(memberArgument)
                    .NormalizeWhitespace();

                yield return InvocationExpression(adapterMethod)
                    .WithArgumentList(arguments)
                    .WithTriviaFrom(invocation);
            }
        }

        private bool TryGetAdapterType(MemberAccessExpressionSyntax member, out TypeSyntax adapterType, out IMethodSymbol method)
        {
            adapterType = default;
            method = default;
            IReadOnlyList<ISymbol> methods = this.model.GetMemberGroup(member.Name);
            if (methods.Count != 1)
            {
                return false;
            }

            method = (IMethodSymbol)methods[0];
            string containingType = method.ContainingType.ToDisplayString();
            return method.IsGenericMethod &&
                   this.typesToReplace.TryGetValue(containingType, out adapterType);
        }
    }
}
