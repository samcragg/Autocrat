namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Autocrat.Compiler;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class ConfigResolverTests
    {
        private readonly Lazy<ConfigResolver> config;
        private readonly ConfigGenerator configGenerator = Substitute.For<ConfigGenerator>();
        private readonly List<INamedTypeSymbol> types = new List<INamedTypeSymbol>();

        private ConfigResolverTests()
        {
            IKnownTypes knownTypes = Substitute.For<IKnownTypes>();
            knownTypes.GetEnumerator().Returns(_ => this.types.GetEnumerator());
            this.config = new Lazy<ConfigResolver>(() => new ConfigResolver(knownTypes, this.configGenerator));
        }

        private ConfigResolver Config => this.config.Value;

        private INamedTypeSymbol AddConfigClass()
        {
            Compilation compilation = CompilationHelper.CompileCode(
                referenceAbstractions: true,
                code: @"
[Autocrat.Abstractions.Configuration]
class ConfigClass
{
}");

            INamedTypeSymbol configClass = compilation.GetTypeByMetadataName("ConfigClass");
            this.types.Add(configClass);
            return configClass;
        }

        public sealed class AccessConfigTests : ConfigResolverTests
        {
            [Fact]
            public void ShouldResolveNestedProperties()
            {
                Compilation compilation = CompilationHelper.CompileCode(
                    referenceAbstractions: true,
                    code: @"
class OtherConfigClass
{
}

[Autocrat.Abstractions.Configuration]
class MainConfigClass
{
    public OtherConfigClass Section { get; set; }
}");

                this.types.Add(compilation.GetTypeByMetadataName("MainConfigClass"));

                ExpressionSyntax result = this.Config.AccessConfig(
                    compilation.GetTypeByMetadataName("OtherConfigClass"));

                result.ToFullString().Should().EndWith(".Root.Section");
            }

            [Fact]
            public void ShouldResolveTheRootConfigurationClass()
            {
                INamedTypeSymbol configClass = this.AddConfigClass();

                ExpressionSyntax result = this.Config.AccessConfig(configClass);

                result.ToFullString().Should().EndWith(".Root");
            }
        }

        public sealed class CreateConfigurationClassTests : ConfigResolverTests
        {
            [Fact]
            public void ShouldHaveTheReadConfigurationMethod()
            {
                this.AddConfigClass();
                this.configGenerator.GetClassFor(null)
                    .ReturnsForAnyArgs(SyntaxFactory.IdentifierName("DeserializerType"));

                ClassDeclarationSyntax result = this.Config.CreateConfigurationClass();
                MethodDeclarationSyntax method = result.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                method.NormalizeWhitespace().ToFullString().Should().StartWith(
                    $"public static void {ConfigResolver.ReadConfigurationMethod}(ref {nameof(Utf8JsonReader)} reader)");
            }

            [Fact]
            public void ShouldReturnNullIfThereAreNoConfigClasses()
            {
                ClassDeclarationSyntax result = this.Config.CreateConfigurationClass();

                result.Should().BeNull();
            }
        }
    }
}
