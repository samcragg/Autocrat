// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Abstractions;
    using Autocrat.Compiler.Logging;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Resolves dependencies representing configuration options.
    /// </summary>
    internal class ConfigResolver
    {
        private const string ConfigurationClassName = "ApplicationConfiguration";
        private const string RootProperty = "Root";
        private const string SingletonProperty = "Instance";
        private readonly INamedTypeSymbol? configurationClass;
        private readonly ExpressionSyntax getConfigRoot;
        private readonly ILogger logger = LogManager.GetLogger();

        private readonly Dictionary<ITypeSymbol, string> properties =
            new Dictionary<ITypeSymbol, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigResolver"/> class.
        /// </summary>
        /// <param name="knownTypes">Contains the discovered types.</param>
        public ConfigResolver(IKnownTypes knownTypes)
        {
            static bool IsConfigurationClass(INamedTypeSymbol symbol)
            {
                return RoslynHelper.FindAttribute<ConfigurationAttribute>(symbol) != null;
            }

            foreach (INamedTypeSymbol classType in knownTypes.Where(IsConfigurationClass))
            {
                this.logger.Info("Found configuration class {0}", classType.Name);
                if (this.configurationClass != null)
                {
                    throw new InvalidOperationException(
                        "Only a single class can be marked as providing configuration.");
                }

                this.configurationClass = classType;
            }

            if (this.configurationClass != null)
            {
                this.AddInjectableProperties(this.configurationClass);
                this.getConfigRoot = AccessProperty(
                    AccessProperty(IdentifierName(ConfigurationClassName), SingletonProperty),
                    RootProperty);
            }
            else
            {
                this.getConfigRoot = LiteralExpression(SyntaxKind.NullLiteralExpression);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigResolver"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected ConfigResolver()
        {
            this.getConfigRoot = null!;
        }

        /// <summary>
        /// Gets an expression for accessing the configuration for the
        /// specified type.
        /// </summary>
        /// <param name="type">The type to get the configuration for.</param>
        /// <returns>
        /// An expression to an object if a configuration type was found;
        /// otherwise, <c>null</c>.
        /// </returns>
        public virtual ExpressionSyntax? AccessConfig(ITypeSymbol type)
        {
            if (SymbolEqualityComparer.Default.Equals(this.configurationClass, type))
            {
                return this.getConfigRoot;
            }
            else if (this.properties.TryGetValue(type, out string property))
            {
                return AccessProperty(this.getConfigRoot, property);
            }
            else
            {
                return null;
            }
        }

        private static ExpressionSyntax AccessProperty(ExpressionSyntax expression, string name)
        {
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                IdentifierName(name));
        }

        private void AddInjectableProperties(INamedTypeSymbol type)
        {
            foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.Type.TypeKind == TypeKind.Class)
                {
                    this.properties.Add(property.Type, property.Name);
                }
            }
        }
    }
}
