namespace Compiler.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class CodeGeneratorTests : IDisposable
    {
        private readonly ServiceFactory factory;
        private readonly CodeGenerator generator;

        private CodeGeneratorTests()
        {
            this.factory = Substitute.For<ServiceFactory>(new object[] { null });
            CodeGenerator.ServiceFactory = _ => this.factory;
            this.generator = new CodeGenerator();

            SyntaxTreeRewriter rewriter = this.factory.CreateSyntaxTreeRewriter();
            rewriter.Generate(null).ReturnsForAnyArgs(ci => ci.Arg<SyntaxTree>());
        }

        public void Dispose()
        {
            CodeGenerator.ServiceFactory = null;
        }

        public sealed class EmitAssemblyTests : CodeGeneratorTests
        {
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
                this.generator.Add(CompilationHelper.CompileCode(@"
public class Test
{
    public UnknownType Invalid { get; set; }
}"));

                this.generator.Invoking(g => g.EmitAssembly(Stream.Null))
                    .Should().Throw<InvalidOperationException>();
            }

            private Assembly EmitCode(string originalCode = "namespace X {}")
            {
                using (var stream = new MemoryStream())
                {
                    this.generator.Add(CompilationHelper.CompileCode(originalCode));
                    this.generator.EmitAssembly(stream);
                    return Assembly.Load(stream.ToArray());
                }
            }
        }

        public sealed class EmitNativeCodeTests : CodeGeneratorTests
        {
            [Fact]
            public void ShouldWriteTheNativeImportCode()
            {
                SyntaxTreeRewriter treeRewriter = this.factory.CreateSyntaxTreeRewriter();
                NativeImportGenerator nativeGenerator = this.factory.GetNativeImportGenerator();
                this.generator.Add(CompilationHelper.CompileCode(@"
public class Test
{
}"));
                this.generator.EmitNativeCode(Stream.Null);

                nativeGenerator.Received().WriteTo(Stream.Null);
                treeRewriter.ReceivedWithAnyArgs().Generate(null);
            }
        }
    }
}
