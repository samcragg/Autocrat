// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NLog;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Creates expressions for initializing new instances of types.
    /// </summary>
    internal class InstanceBuilder
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private static readonly TypeSyntax VarKeyword = ParseTypeName("var");
        private readonly ConstructorResolver constructorResolver;
        private readonly List<StatementSyntax> declarations = new List<StatementSyntax>();
        private readonly InterfaceResolver interfaceResolver;

        private readonly HashSet<string> localNames =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly Dictionary<ITypeSymbol, IdentifierNameSyntax?> variables =
            new Dictionary<ITypeSymbol, IdentifierNameSyntax?>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceBuilder"/> class.
        /// </summary>
        /// <param name="constructorResolver">
        /// Used to resolve the constructor for a type.
        /// </param>
        /// <param name="interfaceResolver">
        /// Used to resolve concrete types for interface types.
        /// </param>
        public InstanceBuilder(
            ConstructorResolver constructorResolver,
            InterfaceResolver interfaceResolver)
        {
            this.constructorResolver = constructorResolver;
            this.interfaceResolver = interfaceResolver;
        }

        /// <summary>
        /// Gets the statements for declaring the local variables.
        /// </summary>
        public virtual IEnumerable<StatementSyntax> LocalDeclarations => this.declarations;

        /// <summary>
        /// Generates a local variable of the specified type, reusing existing
        /// locals where possible.
        /// </summary>
        /// <param name="type">The type of the local variable.</param>
        /// <returns>The name of the local variable.</returns>
        public virtual IdentifierNameSyntax GenerateForType(INamedTypeSymbol type)
        {
            if (!this.variables.TryGetValue(type, out IdentifierNameSyntax? name))
            {
                // Create a marker to avoid cyclic dependencies
                Logger.Debug<string>("Attempting to resolve {typeName}", type.ToDisplayString());
                this.variables.Add(type, null);
                name = this.DeclareLocal(type);
                this.variables[type] = name;
            }

            return name ?? throw new InvalidOperationException(
                "There is a cyclic dependency resolving '" + type.ToDisplayString() + "'");
        }

        private void AddDeclaration(IdentifierNameSyntax name, ExpressionSyntax create)
        {
            VariableDeclaratorSyntax variable =
                VariableDeclarator(name.Identifier)
                    .WithInitializer(EqualsValueClause(create));

            this.declarations.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        VarKeyword,
                        SingletonSeparatedList(variable))));
        }

        private string CreateName(string prefix)
        {
            prefix = char.ToLowerInvariant(prefix[0]) + prefix.Substring(1);

            int count = 0;
            string name = prefix;
            while (this.localNames.Contains(name))
            {
                count++;
                name = prefix + count.ToString(NumberFormatInfo.InvariantInfo);
            }

            return name;
        }

        private IdentifierNameSyntax DeclareArray(INamedTypeSymbol type)
        {
            SeparatedSyntaxList<ExpressionSyntax> instances = default;
            foreach (INamedTypeSymbol classType in this.interfaceResolver.FindClasses(type))
            {
                instances = instances.Add(this.GenerateForType(classType));
            }

            IdentifierNameSyntax nameSyntax = IdentifierName(this.CreateName("array"));
            ArrayTypeSyntax arrayTypeSyntax = ArrayType(
                ParseTypeName(type.ToDisplayString()),
                SingletonList(ArrayRankSpecifier()));

            this.AddDeclaration(
                nameSyntax,
                ArrayCreationExpression(
                    arrayTypeSyntax,
                    InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        instances)));

            return nameSyntax;
        }

        private IdentifierNameSyntax DeclareLocal(INamedTypeSymbol type)
        {
            IdentifierNameSyntax nameSyntax = IdentifierName(this.CreateName(type.Name));
            TypeSyntax typeSyntax = ParseTypeName(type.ToDisplayString());
            SeparatedSyntaxList<SyntaxNode> arguments = SeparatedList(
                this.GetConstructorArguments(type));

            this.AddDeclaration(
                nameSyntax,
                ObjectCreationExpression(typeSyntax)
                    .WithArgumentList(ArgumentList(arguments)));

            return nameSyntax;
        }

        private IEnumerable<SyntaxNode> GetConstructorArguments(INamedTypeSymbol type)
        {
            foreach (ITypeSymbol? parameter in this.constructorResolver.GetParameters(type))
            {
                if (parameter is null)
                {
                    yield return Argument(LiteralExpression(SyntaxKind.NullLiteralExpression));
                }
                else if (parameter is IArrayTypeSymbol arrayType)
                {
                    yield return Argument(this.DeclareArray((INamedTypeSymbol)arrayType.ElementType));
                }
                else if (parameter is INamedTypeSymbol namedType)
                {
                    yield return Argument(this.GenerateForType(namedType));
                }
            }
        }
    }
}
