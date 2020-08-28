namespace Compiler.Tests.CodeGeneration
{
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    public class WorkerRegisterGeneratorTests
    {
        private readonly InstanceBuilder builder;
        private readonly WorkerRegisterGenerator generator;
        private readonly NativeImportGenerator nativeGenerator;
        private readonly List<INamedTypeSymbol> workerTypes = new List<INamedTypeSymbol>();

        private WorkerRegisterGeneratorTests()
        {
            this.builder = Substitute.For<InstanceBuilder>(null, null);
            this.nativeGenerator = Substitute.For<NativeImportGenerator>();

            this.builder.GenerateForType(null)
                .ReturnsForAnyArgs(IdentifierName("instance"));

            this.builder.LocalDeclarations
                .Returns(new[] { EmptyStatement() });

            this.generator = new WorkerRegisterGenerator(() => this.builder, this.workerTypes, this.nativeGenerator);
        }

        public sealed class GenerateTests : WorkerRegisterGeneratorTests
        {
            [Fact]
            public void ShouldCreateAClassWithTheRegisterWorkerTypesMethod()
            {
                this.workerTypes.Add(CompilationHelper.CreateTypeSymbol("class Worker {}"));

                CompilationUnitSyntax compilation = this.generator.Generate();

                ClassDeclarationSyntax classDeclaration =
                    compilation.Members.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<ClassDeclarationSyntax>()
                    .Subject;

                classDeclaration.Identifier.ValueText
                    .Should().Be(WorkerRegisterGenerator.GeneratedClassName);

                classDeclaration.Members.OfType<MethodDeclarationSyntax>()
                    .Should().ContainSingle(m => m.Identifier.ValueText == WorkerRegisterGenerator.GeneratedMethodName);
            }

            [Fact]
            public void ShouldRegisterTheConstructorsAsNativeMethods()
            {
                INamedTypeSymbol testWorker = CompilationHelper.CreateTypeSymbol("class TestWorker {}");
                this.workerTypes.Add(testWorker);
                this.workerTypes.Add(testWorker);
                this.workerTypes.Add(testWorker);

                this.generator.Generate();

                this.nativeGenerator.ReceivedWithAnyArgs(3).RegisterMethod(null, null);
            }
        }
    }
}
