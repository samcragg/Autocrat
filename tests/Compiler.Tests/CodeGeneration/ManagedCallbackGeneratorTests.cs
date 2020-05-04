namespace Compiler.Tests.CodeGeneration
{
    using System.Linq;
    using Autocrat.Compiler.CodeGeneration;
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
        private readonly NativeImportGenerator nativeGenerator;

        private ManagedCallbackGeneratorTests()
        {
            this.instanceBuilder = Substitute.For<InstanceBuilder>(null, null);
            this.instanceBuilder.GenerateForType(null)
                .ReturnsForAnyArgs(SyntaxFactory.IdentifierName("instance"));

            this.nativeGenerator = Substitute.For<NativeImportGenerator>();

            this.generator = new ManagedCallbackGenerator(
                () => this.instanceBuilder,
                this.nativeGenerator);
        }

        public sealed class CreateMethodTests : ManagedCallbackGeneratorTests
        {
            [Fact]
            public void ShouldCallTheMethodOnTheType()
            {
                IMethodSymbol method = CompilationHelper.CreateMethodSymbol("TestMethod");

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
                IMethodSymbol method = CompilationHelper.CreateMethodSymbol("TestMethod", arguments: "int a");

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.NormalizeWhitespace().ParameterList.Parameters.ToString()
                    .Should().Be("int a");
            }

            [Fact]
            public void ShouldHaveTheOriginalReturnType()
            {
                IMethodSymbol method = CompilationHelper.CreateMethodSymbol("TestMethod", returnType: "string");

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.ReturnType.ToString()
                    .Should().Be("string");
            }

            [Fact]
            public void ShouldIncludeTheLocalDeclarations()
            {
                IMethodSymbol method = CompilationHelper.CreateMethodSymbol("TestMethod");
                this.instanceBuilder.LocalDeclarations.Returns(new[] { SyntaxFactory.EmptyStatement() });

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.Body.Statements.First().Should().BeOfType<EmptyStatementSyntax>();
            }

            [Fact]
            public void ShouldReturnNonVoidMethods()
            {
                IMethodSymbol method = CompilationHelper.CreateMethodSymbol("TestMethod", returnType: "int");

                MethodDeclarationSyntax declaration = this.CallCreateMethod(method);

                declaration.Body.Statements.Last()
                    .Should().BeOfType<ReturnStatementSyntax>();
            }

            [Fact]
            public void ShouldReturnTheRegistrationOfTheGeneratedMethod()
            {
                IMethodSymbol method = CompilationHelper.CreateMethodSymbol("TestMethod");
                this.nativeGenerator.RegisterMethod("signature", Arg.Any<string>())
                    .Returns(123);

                int result = this.generator.CreateMethod("signature", method);

                result.Should().Be(123);
            }

            private MethodDeclarationSyntax CallCreateMethod(IMethodSymbol method)
            {
                string name = null;
                this.nativeGenerator.RegisterMethod("native", Arg.Do<string>(s => name = s));
                this.generator.CreateMethod("native", method);

                return this.generator.Methods
                    .FirstOrDefault(m => m.Identifier.ValueText.Equals(name));
            }
        }
    }
}
