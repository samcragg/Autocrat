// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Abstractions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Rewrites an interface to use the static methods on a class.
    /// </summary>
    internal class InterfaceRewriter : CSharpSyntaxRewriter
    {
        // This class allows us to "implement" and interface but also change
        // the arguments. Given the following interface that takes a delegate:
        ////
        //// interface IMyService
        //// {
        ////     void InvokeCallback(NativeDelegate callback);
        //// }
        ////
        // We want to be able to call that method but instead of the delegate
        // pass in the exported method handle, i.e. on usage we'd like to
        // transform this:
        ////
        //// myService.InvokeCallback(this.Method);
        ////
        // into this:
        ////
        //// ClassMarkedAsImplementingMyService.InvokeCallback(123);
        ////
        // This allows the class "implementing" the interface to control how
        // the native method that actually uses the handle gets called (for
        // example, to convert .NET types to native primitive types)
        private readonly NativeDelegateRewriter delegateRewriter;
        private readonly SemanticModel model;

        private readonly Dictionary<string, TypeSyntax> typesToReplace =
            new Dictionary<string, TypeSyntax>(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceRewriter"/> class.
        /// </summary>
        /// <param name="model">Contains the semantic information.</param>
        /// <param name="delegateRewriter">Rewrites delegate arguments.</param>
        public InterfaceRewriter(SemanticModel model, NativeDelegateRewriter delegateRewriter)
        {
            this.model = model;
            this.delegateRewriter = delegateRewriter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceRewriter"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected InterfaceRewriter()
        {
            this.delegateRewriter = null!;
            this.model = null!;
        }

        /// <summary>
        /// Looks for interfaces that the class implements via the
        /// <see cref="RewriteInterfaceAttribute"/>.
        /// </summary>
        /// <param name="classType">The type of the class.</param>
        public virtual void RegisterClass(ITypeSymbol classType)
        {
            AttributeData? rewriteInterface =
                RoslynHelper.FindAttribute<RewriteInterfaceAttribute>(classType);

            if (rewriteInterface != null)
            {
                this.typesToReplace.Add(
                    rewriteInterface.ConstructorArguments[0].Value.ToString(),
                    ParseTypeName(classType.ToDisplayString()));
            }
        }

        /// <inheritdoc />
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax member)
            {
                TypeInfo type = this.model.GetTypeInfo(member.Expression);
                if (this.typesToReplace.TryGetValue(type.Type.ToDisplayString(), out TypeSyntax classType))
                {
                    MemberAccessExpressionSyntax newMethod = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        classType,
                        member.Name);

                    return InvocationExpression(newMethod, this.TransformArguments(node.ArgumentList));
                }
            }

            return base.VisitInvocationExpression(node);
        }

        private ArgumentListSyntax TransformArguments(ArgumentListSyntax argumentList)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = SeparatedList<ArgumentSyntax>();
            foreach (ArgumentSyntax argument in argumentList.Arguments)
            {
                arguments = arguments.Add(this.delegateRewriter.TransformArgument(argument));
            }

            return ArgumentList(arguments);
        }
    }
}
