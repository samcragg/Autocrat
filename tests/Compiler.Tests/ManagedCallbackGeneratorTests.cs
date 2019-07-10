﻿namespace Compiler.Tests
{
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class ManagedCallbackGeneratorTests
    {
        private readonly ManagedCallbackGenerator generator;
        private readonly InstanceBuilder instanceBuilder;

        private ManagedCallbackGeneratorTests()
        {
            this.instanceBuilder = Substitute.For<InstanceBuilder>(null, null);
            this.instanceBuilder.GenerateForType(null)
                .ReturnsForAnyArgs(SyntaxFactory.IdentifierName("instance"));

            this.generator = new ManagedCallbackGenerator(() => this.instanceBuilder);
        }

        private static IMethodSymbol CreateMethodSymbol(string methodName, string arguments = null, string returnType = "void")
        {
            Compilation compilation = CompilationHelper.CompileCode("partial class TestClass { " +
                returnType + " " + methodName + "(" + arguments + "){}}");
            SyntaxTree tree = compilation.SyntaxTrees.First();
            MethodDeclarationSyntax method = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            SemanticModel model = compilation.GetSemanticModel(tree);
            return model.GetDeclaredSymbol(method);
        }

        public sealed class CreateMethodTests : ManagedCallbackGeneratorTests
        {
            [Fact]
            public void ShouldCallTheMethodOnTheType()
            {
                IMethodSymbol method = CreateMethodSymbol("TestMethod");

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.Body
                    .DescendantNodes()
                    .OfType<MemberAccessExpressionSyntax>()
                    .Should().ContainSingle()
                    .Which.Name.ToString().Should().Be("TestMethod");
            }

            [Fact]
            public void ShouldHaveTheOriginalArguments()
            {
                IMethodSymbol method = CreateMethodSymbol("TestMethod", arguments: "int a");

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.NormalizeWhitespace().ParameterList.Parameters.ToString()
                    .Should().Be("int a");
            }

            [Fact]
            public void ShouldHaveTheOriginalReturnType()
            {
                IMethodSymbol method = CreateMethodSymbol("TestMethod", returnType: "string");

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.ReturnType.ToString()
                    .Should().Be("string");
            }

            [Fact]
            public void ShouldIncludeTheLocalDeclarations()
            {
                IMethodSymbol method = CreateMethodSymbol("TestMethod");
                this.instanceBuilder.LocalDeclarations.Returns(new[] { SyntaxFactory.EmptyStatement() });

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.Body.Statements.First().Should().BeOfType<EmptyStatementSyntax>();
            }

            [Fact]
            public void ShouldReturnNonVoidMethods()
            {
                IMethodSymbol method = CreateMethodSymbol("TestMethod", returnType: "int");

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.Body.Statements.Last()
                    .Should().BeOfType<ReturnStatementSyntax>();
            }

            [Fact]
            public void ShouldReturnTheNameOfTheGeneratedMethod()
            {
                IMethodSymbol method = CreateMethodSymbol("TestMethod");

                SimpleNameSyntax result = this.generator.CreateMethod(method);

                result.Identifier.ValueText.Should().Contain("TestMethod");
            }

            private MethodDeclarationSyntax CallCreateMethod(IMethodSymbol method)
            {
                SimpleNameSyntax result = this.generator.CreateMethod(method);
                return this.FindMethod(result);
            }

            private MethodDeclarationSyntax FindMethod(SimpleNameSyntax name)
            {
                return this.generator.GetCompilationUnit()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.ValueText.Equals(name.Identifier.ValueText));
            }
        }

        public sealed class GetCompilationUnitTests : ManagedCallbackGeneratorTests
        {
            [Fact]
            public void ShouldReturnAllTheAddedMethods()
            {
                SimpleNameSyntax method1 = this.generator.CreateMethod(CreateMethodSymbol("Method1"));
                SimpleNameSyntax method2 = this.generator.CreateMethod(CreateMethodSymbol("Method2"));

                CompilationUnitSyntax result = this.generator.GetCompilationUnit();
                string[] methods = result.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Select(m => m.Identifier.ValueText)
                    .ToArray();

                methods.Should().BeEquivalentTo(
                    method1.Identifier.ValueText,
                    method2.Identifier.ValueText);
            }
        }
    }
}
