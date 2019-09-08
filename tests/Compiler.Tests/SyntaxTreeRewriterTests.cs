namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Abstractions;
    using Autocrat.Compiler;
    using Microsoft.CodeAnalysis;
    using NSubstitute;
    using Xunit;

    public class SyntaxTreeRewriterTests
    {
        private readonly NativeRegisterRewriter nativeRewriter;

        private SyntaxTreeRewriterTests()
        {
            this.nativeRewriter = Substitute.For<NativeRegisterRewriter>();
            this.nativeRewriter.Visit(null)
                .ReturnsForAnyArgs(ci => ci.Arg<SyntaxNode>());
        }

        private SyntaxTree RewriteCode(string code, params Type[] runtimeTypes)
        {
            Compilation compilation = CompilationHelper.CompileCode(code);

            IEnumerable<INamedTypeSymbol> typeSymbols =
                runtimeTypes.Select(t => compilation.GetTypeByMetadataName(t.FullName));

            IKnownTypes knownTypes = Substitute.For<IKnownTypes>();
            knownTypes.GetEnumerator().Returns(typeSymbols.GetEnumerator());

            var treeRewriter = new SyntaxTreeRewriter(
                compilation,
                _ => this.nativeRewriter,
                knownTypes);

            return treeRewriter.Generate(compilation.SyntaxTrees.Single());
        }

        public sealed class GenerateTests : SyntaxTreeRewriterTests
        {
            public interface ITestAdapter
            {
            }

            [Fact]
            public void ShouldAddTheKnownTypes()
            {
                this.RewriteCode(@"public class C { }", typeof(TestAdapter));

                this.nativeRewriter.Received().AddReplacement(
                    Arg.Is<ITypeSymbol>(t => t.Name == nameof(ITestAdapter)),
                    Arg.Is<ITypeSymbol>(t => t.Name == nameof(TestAdapter)));
            }

            [NativeAdapter]
            public class TestAdapter : ITestAdapter
            {
            }
        }
    }
}
