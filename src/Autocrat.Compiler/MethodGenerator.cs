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
    /// Provides a framework for creating native callable methods.
    /// </summary>
    internal abstract class MethodGenerator
    {
        private readonly string generatedClassName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodGenerator"/> class.
        /// </summary>
        /// <param name="generatedClassName">The name for the generated class.</param>
        protected MethodGenerator(string generatedClassName)
        {
            this.generatedClassName = generatedClassName;
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
        /// Creates a public static method that is exported to native code.
        /// </summary>
        /// <param name="name">The method identifier.</param>
        /// <param name="body">The method body.</param>
        /// <param name="returnType">Option return type.</param>
        /// <returns>The method declaration syntax.</returns>
        protected static MemberDeclarationSyntax CreateMethod(string name, BlockSyntax body, TypeSyntax? returnType = null)
        {
            SyntaxList<AttributeListSyntax> attributes = SingletonList(AttributeList(SingletonSeparatedList(
                    RoslynHelper.CreateNativeCallableAttribute(name))));

            MethodDeclarationSyntax method = MethodDeclaration(
                returnType ?? PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier(name));

            return method
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .WithAttributeLists(attributes)
                .WithBody(body);
        }

        /// <summary>
        /// Creates the methods for the generated class.
        /// </summary>
        /// <returns>A sequence of methods to add.</returns>
        protected abstract IEnumerable<MemberDeclarationSyntax> GetMethods();

        private MemberDeclarationSyntax CreateClass()
        {
            return ClassDeclaration(this.generatedClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(this.GetMethods().ToArray());
        }
    }
}
