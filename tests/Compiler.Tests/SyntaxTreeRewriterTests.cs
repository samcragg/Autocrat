namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Compiler;
    using Microsoft.CodeAnalysis;
    using NSubstitute;
    using Xunit;

    public class SyntaxTreeRewriterTests
    {
        private readonly InterfaceRewriter interfaceRewriter;

        private SyntaxTreeRewriterTests()
        {
            this.interfaceRewriter = Substitute.For<InterfaceRewriter>();
            this.interfaceRewriter.Visit(null)
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
                _ => this.interfaceRewriter,
                knownTypes);

            return treeRewriter.Generate(compilation.SyntaxTrees.Single());
        }

        public sealed class GenerateTests : SyntaxTreeRewriterTests
        {
            [Fact]
            public void ShouldAddTheKnownTypes()
            {
                this.RewriteCode(@"public class C { }", typeof(TestAdapter));

                this.interfaceRewriter.Received().RegisterClass(
                    Arg.Is<ITypeSymbol>(t => t.Name == nameof(TestAdapter)));
            }

            public class TestAdapter
            {
            }
        }
    }
}
