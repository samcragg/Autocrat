// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
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
        private readonly IKnownTypes knownTypes;
        private readonly SemanticModel model;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceRewriter"/> class.
        /// </summary>
        /// <param name="model">Contains the semantic information.</param>
        /// <param name="knownTypes">Contains the discovered types.</param>
        /// <param name="delegateRewriter">Rewrites delegate arguments.</param>
        public InterfaceRewriter(SemanticModel model, IKnownTypes knownTypes, NativeDelegateRewriter delegateRewriter)
        {
            this.model = model;
            this.knownTypes = knownTypes;
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
            this.knownTypes = null!;
            this.model = null!;
        }

        /// <inheritdoc />
        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax member)
            {
                TypeInfo type = this.model.GetTypeInfo(member.Expression);
                if (type.Type != null)
                {
                    ITypeSymbol? classType = this.knownTypes.FindClassForInterface(type.Type);
                    if (classType != null)
                    {
                        MemberAccessExpressionSyntax newMethod = MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ParseTypeName(classType.ToDisplayString()),
                            member.Name);

                        return InvocationExpression(newMethod, this.TransformArguments(node.ArgumentList));
                    }
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
