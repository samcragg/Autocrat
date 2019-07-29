namespace Compiler.Tests
{
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Xunit;

    public class NamedTypeVisitorTests
    {
        private readonly NamedTypeVisitor visitor = new NamedTypeVisitor();

        public sealed class VisitTests : NamedTypeVisitorTests
        {
            [Fact]
            public void ShouldExtractDeclaredTypes()
            {
                Compilation compilation = CompilationHelper.CompileCode(@"
namespace TestNamespace
{
    public class ExampleClass
    {
    }
}");
                this.visitor.Visit(GetNamespace(compilation, "TestNamespace"));

                this.visitor.Types.Should().ContainSingle()
                    .Which.Should().Be(compilation.GetTypeByMetadataName("TestNamespace.ExampleClass"));
            }

            [Fact]
            public void ShouldExtractTypesInNestedNamespaces()
            {
                Compilation compilation = CompilationHelper.CompileCode(@"
namespace OuterNamespace.InnerNamespace
{
    public class ExampleClass
    {
    }
}");
                this.visitor.Visit(GetNamespace(compilation, "OuterNamespace"));

                this.visitor.Types.Should().ContainSingle()
                    .Which.Should().Be(compilation.GetTypeByMetadataName("OuterNamespace.InnerNamespace.ExampleClass"));
            }

            private static INamespaceSymbol GetNamespace(Compilation compilation, string name)
            {
                return compilation.GlobalNamespace.GetNamespaceMembers()
                    .Single(n => n.Name == name);
            }
        }
    }
}
