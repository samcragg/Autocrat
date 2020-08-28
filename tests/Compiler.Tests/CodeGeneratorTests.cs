namespace Compiler.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Autocrat.Compiler;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class CodeGeneratorTests
    {
        private readonly ServiceFactory factory;

        private CodeGeneratorTests()
        {
            this.factory = Substitute.For<ServiceFactory>(new object[] { null });

            SyntaxTreeRewriter rewriter = this.factory.CreateSyntaxTreeRewriter();
            rewriter.Generate(null).ReturnsForAnyArgs(ci => ci.Arg<SyntaxTree>());
        }

        private CodeGenerator CreateGenerator(string code = null, bool allowErrors = false)
        {
            Compilation compilation = CompilationHelper.CompileCode(
                code ?? "namespace X {}",
                allowErrors,
                referenceAbstractions: true);
            return new CodeGenerator(this.factory, compilation);
        }

        public sealed class EmitAssemblyTests : CodeGeneratorTests
        {
            [Fact]
            public void ShouldIncludeTheConfigurationClasses()
            {
                CompilationUnitSyntax serializers = SyntaxFactory.ParseCompilationUnit(
                    @"public class TestSerializer { }");

                var configClass = (ClassDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
                    @"public class TestConfig { }");

                ConfigResolver resolver = this.factory.GetConfigResolver();
                resolver.CreateConfigurationClass().Returns(configClass);

                ConfigGenerator generator = this.factory.GetConfigGenerator();
                generator.Generate().Returns(serializers);

                Assembly assembly = this.EmitCode();

                assembly.GetType("TestConfig").Should().NotBeNull();
                assembly.GetType("TestSerializer").Should().NotBeNull();
            }

            [Fact]
            public void ShouldIncludeTheOriginalCode()
            {
                Assembly assembly = this.EmitCode(@"namespace Original
{
    public class OriginalClass
    {
    }
}");

                assembly.GetType("Original.OriginalClass").Should().NotBeNull();
            }

            [Fact]
            public void ShouldRewriteTheInitializers()
            {
                CompilationUnitSyntax unit = SyntaxFactory.ParseCompilationUnit(@"namespace Initialization
{
    public class Test
    {
    }
}");
                InitializerGenerator initializer = this.factory.CreateInitializerGenerator();
                initializer.HasCode.Returns(true);
                initializer.Generate().Returns(unit);

                Assembly assembly = this.EmitCode();

                assembly.GetType("Initialization.Test").Should().NotBeNull();
            }

            [Fact]
            public void ShouldRewriteTheSytnaxTrees()
            {
                SyntaxTree tree = CompilationHelper.CompileCode(@"public class Rewritten { }")
                    .SyntaxTrees
                    .Single();

                SyntaxTreeRewriter rewriter = this.factory.CreateSyntaxTreeRewriter();
                rewriter.Generate(null).ReturnsForAnyArgs(tree);

                Assembly assembly = this.EmitCode();

                assembly.GetType("Rewritten").Should().NotBeNull();
            }

            [Fact]
            public void ShouldThrowIfTheCodeFailsToCompile()
            {
                CodeGenerator generator = this.CreateGenerator(@"
public class Test
{
    public UnknownType Invalid { get; set; }
}",
                    allowErrors: true);

                generator.Invoking(g => g.EmitAssembly(Stream.Null, Stream.Null))
                    .Should().Throw<InvalidOperationException>();
            }

            private Assembly EmitCode(string originalCode = null)
            {
                using var stream = new MemoryStream();
                CodeGenerator generator = this.CreateGenerator(originalCode);
                generator.EmitAssembly(stream, Stream.Null);
                return Assembly.Load(stream.ToArray());
            }
        }

        public sealed class EmitNativeCodeTests : CodeGeneratorTests
        {
            [Fact]
            public void ShouldWriteTheNativeImportCode()
            {
                NativeImportGenerator nativeGenerator = this.factory.GetNativeImportGenerator();
                CodeGenerator generator = this.CreateGenerator("public class Test { }");

                generator.EmitNativeCode(Stream.Null);

                nativeGenerator.Received().WriteTo(Stream.Null);
            }
        }
    }
}
