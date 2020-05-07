// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NLog;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Generates the code for reading runtime configuration.
    /// </summary>
    internal class ConfigGenerator
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<MemberDeclarationSyntax> deserializerClasses =
            new List<MemberDeclarationSyntax>();

        private readonly Dictionary<ITypeSymbol, IdentifierNameSyntax?> deserializers =
            new Dictionary<ITypeSymbol, IdentifierNameSyntax?>();

        /// <summary>
        /// Gets or sets the method used to create the deserializer builder.
        /// </summary>
        internal static Func<ConfigGenerator, ITypeSymbol, JsonDeserializerBuilder> CreateBuilder { get; set; }
            = (cg, ct) => new JsonDeserializerBuilder(cg, ct);

        /// <summary>
        /// Generates the deserialization classes.
        /// </summary>
        /// <returns>A new compilation unit.</returns>
        public virtual CompilationUnitSyntax Generate()
        {
            return CompilationUnit()
                .WithUsings(List(JsonDeserializerBuilder.GetUsingStatements()))
                .WithMembers(List(this.deserializerClasses));
        }

        /// <summary>
        /// Gets the deserializer for the specified type.
        /// </summary>
        /// <param name="type">The type for the deserializer to return.</param>
        /// <returns>The name of the deserializer class.</returns>
        public virtual IdentifierNameSyntax GetClassFor(ITypeSymbol type)
        {
            if (!this.deserializers.TryGetValue(type, out IdentifierNameSyntax? name))
            {
                // Create a marker to avoid cyclic dependencies
                Logger.Debug<string>("Generating deserializer for {typeName}", type.ToDisplayString());
                this.deserializers.Add(type, null);
                name = this.GenerateClass(type);
                this.deserializers[type] = name;
            }

            return name ?? throw new InvalidOperationException(
                "There is a cyclic dependency deserializing '" + type.ToDisplayString() + "'");
        }

        private IdentifierNameSyntax GenerateClass(ITypeSymbol type)
        {
            JsonDeserializerBuilder builder = CreateBuilder(this, type);
            foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.SetMethod != null)
                {
                    builder.AddProperty(property);
                }
            }

            ClassDeclarationSyntax generated = builder.GenerateClass();
            this.deserializerClasses.Add(generated);
            return IdentifierName(generated.Identifier);
        }
    }
}
