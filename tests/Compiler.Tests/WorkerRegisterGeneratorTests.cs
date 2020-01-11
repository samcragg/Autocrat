namespace Compiler.Tests
{
    using System.Collections.Generic;
    using Autocrat.Compiler;
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
            private const string GeneratedMethodName = "RegisterWorkerTypes";

            [Fact]
            public void ShouldCreateAClassWithTheRegisterWorkerTypesMethod()
            {
                this.workerTypes.Add(CompilationHelper.CreateTypeSymbol("class Worker {}"));

                CompilationUnitSyntax compilation = this.generator.Generate();

                MemberDeclarationSyntax member =
                    compilation.Members.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<ClassDeclarationSyntax>()
                    .Which.Members.Should().ContainSingle(m => ((MethodDeclarationSyntax)m).Identifier.ValueText == GeneratedMethodName)
                    .Subject;

                CompilationHelper.AssertExportedAs(member, GeneratedMethodName);
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
