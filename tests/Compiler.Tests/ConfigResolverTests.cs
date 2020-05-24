namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class ConfigResolverTests
    {
        private readonly Lazy<ConfigResolver> config;
        private readonly List<INamedTypeSymbol> types = new List<INamedTypeSymbol>();

        private ConfigResolverTests()
        {
            IKnownTypes knownTypes = Substitute.For<IKnownTypes>();
            knownTypes.GetEnumerator().Returns(_ => this.types.GetEnumerator());
            this.config = new Lazy<ConfigResolver>(() => new ConfigResolver(knownTypes));
        }

        private ConfigResolver Config => this.config.Value;

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
                Compilation compilation = CompilationHelper.CompileCode(
                    referenceAbstractions: true,
                    code: @"
[Autocrat.Abstractions.Configuration]
class ConfigClass
{
}");

                INamedTypeSymbol configClass = compilation.GetTypeByMetadataName("ConfigClass");
                this.types.Add(configClass);

                ExpressionSyntax result = this.Config.AccessConfig(configClass);

                result.ToFullString().Should().EndWith(".Root");
            }
        }
    }
}
