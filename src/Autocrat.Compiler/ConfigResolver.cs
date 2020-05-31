// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Autocrat.Abstractions;
    using Autocrat.Compiler.CodeGeneration;
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
        /// <summary>
        /// The name of the generated class.
        /// </summary>
        internal const string ConfigurationClassName = "ApplicationConfiguration";

        /// <summary>
        /// The name of the generated method that reads the configuration.
        /// </summary>
        internal const string ReadConfigurationMethod = "ReadConfig";

        private const string RootProperty = "Root";
        private const string SingletonProperty = "Instance";
        private readonly ConfigGenerator configGenerator;
        private readonly INamedTypeSymbol? configurationClass;
        private readonly ExpressionSyntax getConfigRoot;
        private readonly ILogger logger = LogManager.GetLogger();

        private readonly Dictionary<ITypeSymbol, string> properties =
            new Dictionary<ITypeSymbol, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigResolver"/> class.
        /// </summary>
        /// <param name="knownTypes">Contains the discovered types.</param>
        /// <param name="configGenerator">Generate the configuration deserializers.</param>
        public ConfigResolver(IKnownTypes knownTypes, ConfigGenerator configGenerator)
        {
            this.configGenerator = configGenerator;
            this.configurationClass = this.FindConfigurationClass(knownTypes);

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
            this.configGenerator = null!;
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
            else if (this.properties.TryGetValue(type, out string? property))
            {
                return AccessProperty(this.getConfigRoot, property);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates the application configuration class.
        /// </summary>
        /// <returns>
        /// The class declaration, or <c>null</c> if no configuration class
        /// was found.
        /// </returns>
        public virtual ClassDeclarationSyntax? CreateConfigurationClass()
        {
            if (this.configurationClass == null)
            {
                return null;
            }

            IdentifierNameSyntax className = IdentifierName(ConfigurationClassName);
            return ClassDeclaration(className.Identifier)
                .WithMembers(List(new[]
                {
                    this.CreateConstructor(this.configurationClass),
                    CreateReadMethod(),

                    //// public static ApplicationConfiguration Instance { get; set; }
                    CreateProperty(
                        new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
                        className,
                        SingletonProperty,
                        new[] { SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration }),

                    //// public ConfigType Root { get; }
                    CreateProperty(
                        new[] { SyntaxKind.PublicKeyword },
                        IdentifierName(RoslynHelper.GetString(this.configurationClass)),
                        RootProperty,
                        new[] { SyntaxKind.GetAccessorDeclaration }),
                }));
        }

        private static ExpressionSyntax AccessProperty(ExpressionSyntax expression, string name)
        {
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                IdentifierName(name));
        }

        private static MemberDeclarationSyntax CreateProperty(
            SyntaxKind[] modifers,
            TypeSyntax type,
            string name,
            SyntaxKind[] accessors)
        {
            return PropertyDeclaration(type, name)
                .WithModifiers(TokenList(
                    Array.ConvertAll(modifers, Token)))
                .WithAccessorList(AccessorList(List(
                    Array.ConvertAll(accessors, AccessorDeclaration))));
        }

        private static MemberDeclarationSyntax CreateReadMethod()
        {
            IdentifierNameSyntax reader = IdentifierName("reader");
            BlockSyntax body = Block(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(SingletonProperty),
                        ObjectCreationExpression(
                            IdentifierName(ConfigurationClassName),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(reader).WithRefKindKeyword(
                                        Token(SyntaxKind.RefKeyword)))),
                            null))));

            return MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                ReadConfigurationMethod)
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Utf8JsonReaderParameter(reader))))
                .WithBody(body);
        }

        private static ParameterSyntax Utf8JsonReaderParameter(IdentifierNameSyntax reader)
        {
            return Parameter(reader.Identifier)
                .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                .WithType(IdentifierName(nameof(Utf8JsonReader)));
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

        private MemberDeclarationSyntax CreateConstructor(ITypeSymbol configType)
        {
            static StatementSyntax CreateLocal(TypeSyntax type, IdentifierNameSyntax name)
            {
                return LocalDeclarationStatement(
                    VariableDeclaration(
                        IdentifierName("var"),
                        SingletonSeparatedList(
                            VariableDeclarator(name.Identifier)
                            .WithInitializer(EqualsValueClause(
                                ObjectCreationExpression(type))))));
            }

            //// public ApplicationConfiguration(ref Utf8JsonReader reader)
            ////     var deserializer = new MyConfigDeserializer()
            ////     this.Root = deserializer.Read(ref reader)
            IdentifierNameSyntax deserializerType = this.configGenerator.GetClassFor(configType);
            IdentifierNameSyntax deserializer = IdentifierName("deserializer");
            IdentifierNameSyntax reader = IdentifierName("reader");

            BlockSyntax body = Block(
                CreateLocal(deserializerType, deserializer),
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        AccessProperty(ThisExpression(), RootProperty),
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                deserializer,
                                IdentifierName(JsonDeserializerBuilder.ReadMethodName)),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(reader).WithRefKindKeyword(
                                        Token(SyntaxKind.RefKeyword))))))));

            return ConstructorDeclaration(ConfigurationClassName)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Utf8JsonReaderParameter(reader))))
                .WithBody(body);
        }

        private INamedTypeSymbol? FindConfigurationClass(IKnownTypes knownTypes)
        {
            static bool IsConfigurationClass(INamedTypeSymbol symbol)
            {
                return RoslynHelper.FindAttribute<ConfigurationAttribute>(symbol) != null;
            }

            INamedTypeSymbol? foundType = null;
            foreach (INamedTypeSymbol classType in knownTypes.Where(IsConfigurationClass))
            {
                this.logger.Info("Found configuration class {0}", classType.Name);
                if (foundType != null)
                {
                    throw new InvalidOperationException(
                        "Only a single class can be marked as providing configuration.");
                }

                foundType = classType;
            }

            return foundType;
        }
    }
}
