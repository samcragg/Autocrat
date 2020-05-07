namespace Compiler.Tests.CodeGeneration
{
    using System;
    using System.Linq;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using NSubstitute.ReturnsExtensions;
    using Xunit;

    public class ConfigGeneratorTests : IDisposable
    {
        private readonly JsonDeserializerBuilder builder;
        private readonly ConfigGenerator generator;

        private ConfigGeneratorTests()
        {
            this.builder = Substitute.For<JsonDeserializerBuilder>();
            ConfigGenerator.CreateBuilder = _ => this.builder;

            this.builder.GenerateClass().Returns(SyntaxFactory.ClassDeclaration("TestClass"));

            this.generator = new ConfigGenerator();
        }

        public void Dispose()
        {
            ConfigGenerator.CreateBuilder = null;
        }

        public sealed class GenerateTests : ConfigGeneratorTests
        {
            [Fact]
            public void ShouldOutputAllTheDeserializers()
            {
                this.generator.GetClassFor(CompilationHelper.CreateTypeSymbol(@"class OneClass { }"));
                this.generator.GetClassFor(CompilationHelper.CreateTypeSymbol(@"class TwoClass { }"));

                CompilationUnitSyntax compilation = this.generator.Generate();

                compilation.Members
                    .OfType<ClassDeclarationSyntax>()
                    .Should().HaveCount(2);
            }
        }

        public sealed class GetClassForTests : ConfigGeneratorTests
        {
            [Fact]
            public void ShouldAddWritableProperties()
            {
                INamedTypeSymbol classType = CompilationHelper.CreateTypeSymbol(@"class TestClass
{
    public string ReadOnly { get; }
    public string ReadAndWrite { get; set; }
}");
                this.generator.GetClassFor(classType);

                this.builder.DidNotReceive().AddProperty(Arg.Is<IPropertySymbol>(p => p.Name == "ReadOnly"));
                this.builder.Received().AddProperty(Arg.Is<IPropertySymbol>(p => p.Name == "ReadAndWrite"));
            }

            [Fact]
            public void ShouldCacheClassTypes()
            {
                INamedTypeSymbol classType = CompilationHelper.CreateTypeSymbol(@"class TestClass
{
    public string Property { get; set; }
}");
                IdentifierNameSyntax result1 = this.generator.GetClassFor(classType);
                IdentifierNameSyntax result2 = this.generator.GetClassFor(classType);

                this.builder.ReceivedWithAnyArgs(1).AddProperty(null);
                result1.Should().BeSameAs(result2);
            }

            [Fact]
            public void ShouldCheckForCyclicProperties()
            {
                INamedTypeSymbol cyclic = CompilationHelper.CreateTypeSymbol(@"class Cyclic
{
    public Cyclic Parent { get; set; }
}");
                this.builder.GenerateClass()
                    .ReturnsNull()
                    .AndDoes(ci => this.generator.GetClassFor(cyclic));

                this.generator.Invoking(g => g.GetClassFor(cyclic))
                    .Should().Throw<InvalidOperationException>()
                    .WithMessage("*cyclic*");
            }
        }
    }
}
