namespace Compiler.Tests
{
    using System;
    using System.IO;
    using System.Reflection;
    using Autocrat.Compiler;
    using FluentAssertions;
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
            this.factory.CreateInitializerGenerator()
                    .Generate().Returns(SyntaxFactory.ParseCompilationUnit("namespace X {}"));

            CodeGenerator.ServiceFactory = _ => this.factory;
            this.generator = new CodeGenerator();
        }

        public void Dispose()
        {
            CodeGenerator.ServiceFactory = null;
        }

        public sealed class EmitTests : CodeGeneratorTests
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
                this.factory.CreateInitializerGenerator()
                    .Generate().Returns(unit);

                Assembly assembly = this.EmitCode();

                assembly.GetType("Initialization.Test").Should().NotBeNull();
            }

            private Assembly EmitCode(string originalCode = "namespace X {}")
            {
                using (var stream = new MemoryStream())
                {
                    this.generator.Emit(stream, CompilationHelper.CompileCode(originalCode));
                    return Assembly.Load(stream.ToArray());
                }
            }
        }
    }
}
