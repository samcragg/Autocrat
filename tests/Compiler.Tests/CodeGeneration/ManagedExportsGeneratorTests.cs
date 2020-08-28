using Autocrat.NativeAdapters;

namespace Compiler.Tests.CodeGeneration
{
    using System.Linq;
    using Autocrat.Compiler;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Xunit;

    public class ManagedExportsGeneratorTests
    {
        private readonly ManagedExportsGenerator generator = new ManagedExportsGenerator();

        public sealed class GenerateTests : ManagedExportsGeneratorTests
        {
            [Fact]
            public void ShouldCreateAClassWithTheRegisterManagedTypesMethod()
            {
                CompilationUnitSyntax compilation = this.generator.Generate();

                ClassDeclarationSyntax classDeclaration =
                    compilation.Members.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<ClassDeclarationSyntax>()
                    .Subject;

                classDeclaration.Identifier.ValueText
                    .Should().Be(ManagedExportsGenerator.GeneratedClassName);

                MethodDeclarationSyntax methodDeclaration =
                    classDeclaration.Members.OfType<MethodDeclarationSyntax>()
                    .Should().ContainSingle(m => m.Identifier.ValueText == ManagedExportsGenerator.GeneratedMethodName)
                    .Subject;

                CompilationHelper.AssertExportedAs(methodDeclaration, ManagedExportsGenerator.GeneratedMethodName);
            }

            [Fact]
            public void ShouldInitializeTheConfigService()
            {
                this.generator.IncludeConfig = true;

                string generatedCode = this.generator.Generate()
                    .NormalizeWhitespace().ToFullString();

                string generatedConfigMethod =
                    $"{ConfigResolver.ConfigurationClassName}.{ConfigResolver.ReadConfigurationMethod}";
                generatedCode.Should().Contain(
                    $"{nameof(ConfigService)}.{nameof(ConfigService.Initialize)}({generatedConfigMethod})");
            }

            [Fact]
            public void ShouldNotInitializeTheConfigServiceIfIncludeCondigIsFalse()
            {
                this.generator.IncludeConfig = false;

                string generatedCode = this.generator.Generate()
                    .NormalizeWhitespace().ToFullString();

                // This class doesn't exist if IncludeConfig is false, so we
                // mustn't mention it
                generatedCode.Should().NotContain(ConfigResolver.ConfigurationClassName);
            }

            [Fact]
            public void ShouldRegisterTheWorkerTypes()
            {
                string generatedCode = this.generator.Generate()
                    .NormalizeWhitespace().ToFullString();

                generatedCode.Should().Contain(
                    $"{WorkerRegisterGenerator.GeneratedClassName}.{WorkerRegisterGenerator.GeneratedMethodName}()");
            }
        }
    }
}
