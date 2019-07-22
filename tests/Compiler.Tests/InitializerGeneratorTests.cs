namespace Compiler.Tests
{
    using System;
    using System.Linq;
    using Autocrat.Abstractions;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class InitializerGeneratorTests
    {
        private readonly InstanceBuilder builder;
        private readonly InitializerGenerator initializer;

        private InitializerGeneratorTests()
        {
            this.builder = Substitute.For<InstanceBuilder>(null, null);

            this.builder.GenerateForType(null)
                .ReturnsForAnyArgs(SyntaxFactory.IdentifierName("instance"));

            this.builder.LocalDeclarations
                .Returns(new[] { SyntaxFactory.EmptyStatement() });

            this.initializer = new InitializerGenerator(this.builder);
        }

        public sealed class GenerateTests : InitializerGeneratorTests
        {
            private const string GeneratedMethodName = nameof(IInitializer.OnConfigurationLoaded);

            [Fact]
            public void ShouldCreateAClassWithAnNativeCallableMethod()
            {
                this.initializer.AddClass(CreateClass());
                CompilationUnitSyntax compilation = this.initializer.Generate();
                SeparatedSyntaxList<AttributeArgumentSyntax> arguments =
                    compilation.Members.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<ClassDeclarationSyntax>()
                    .Which.Members.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<MethodDeclarationSyntax>()
                    .Which.AttributeLists.Should().ContainSingle()
                    .Which.Attributes.Should().ContainSingle()
                    .Subject.ArgumentList.Arguments;

                arguments.Select(x => x.NormalizeWhitespace().ToString())
                    .Should().Contain("EntryPoint = \"" + GeneratedMethodName + "\"");
            }

            [Fact]
            public void ShouldThrowIfNoClassesAreAdded()
            {
                this.initializer.Invoking(x => x.Generate())
                    .Should().Throw<InvalidOperationException>();
            }

            private static INamedTypeSymbol CreateClass()
            {
                Compilation compilation = CompilationHelper.CompileCode(
"class TestClass : Autocrat.Abstractions." + nameof(IInitializer) + @"
{
    void " + GeneratedMethodName + @"()
    {
    }
}");
                SyntaxTree tree = compilation.SyntaxTrees.First();
                TypeDeclarationSyntax type = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .First();

                SemanticModel model = compilation.GetSemanticModel(tree);
                return model.GetDeclaredSymbol(type);
            }
        }
    }
}
