// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Provides a framework for creating native callable methods.
    /// </summary>
    internal abstract class MethodGenerator
    {
        private readonly string generatedClassName;
        private readonly string generatedMethodName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodGenerator"/> class.
        /// </summary>
        /// <param name="generatedClassName">The name for the generated class.</param>
        /// <param name="generatedMethodName">The name for the generated method.</param>
        protected MethodGenerator(string generatedClassName, string generatedMethodName)
        {
            this.generatedClassName = generatedClassName;
            this.generatedMethodName = generatedMethodName;
        }

        /// <summary>
        /// Gets a value indicating whether any code exists to generate.
        /// </summary>
        public abstract bool HasCode { get; }

        /// <summary>
        /// Generates the initialization code.
        /// </summary>
        /// <returns>A new compilation unit.</returns>
        public virtual CompilationUnitSyntax Generate()
        {
            if (!this.HasCode)
            {
                throw new InvalidOperationException("No code to generate.");
            }

            UsingDirectiveSyntax nativeInterop = UsingDirective(
                ParseName("System.Runtime.InteropServices"));

            return CompilationUnit()
                .WithUsings(SingletonList(nativeInterop))
                .WithMembers(SingletonList(this.CreateClass()));
        }

        /// <summary>
        /// Creates the body for the method.
        /// </summary>
        /// <returns>The code for the method.</returns>
        protected abstract BlockSyntax CreateMethodBody();

        private MemberDeclarationSyntax CreateClass()
        {
            MethodDeclarationSyntax method = MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier(this.generatedMethodName))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(
                    RoslynHelper.CreateNativeCallableAttribute(this.generatedMethodName)))))
                .WithBody(this.CreateMethodBody());

            return ClassDeclaration(this.generatedClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(method);
        }
    }
}
