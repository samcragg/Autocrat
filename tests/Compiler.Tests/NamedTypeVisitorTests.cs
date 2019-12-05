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
            public void ShouldExtractRewrittenInterfaces()
            {
                Compilation compilation = CompilationHelper.CompileCode(@"
namespace Autocrat.Abstractions
{
    public sealed class RewriteInterfaceAttribute : System.Attribute
    {
        public RewriteInterfaceAttribute(System.Type interfaceType) { }
    }
}

namespace TestNamespace
{
    interface IFakeInterface
    {
    }

    [Autocrat.Abstractions.RewriteInterface(typeof(IFakeInterface))]
    static class FakeClass
    {
    }
}");
                INamedTypeSymbol interfaceType = compilation.GetTypeByMetadataName("TestNamespace.IFakeInterface");

                this.visitor.Visit(GetNamespace(compilation, "TestNamespace"));
                ITypeSymbol result = this.visitor.Types.FindClassForInterface(interfaceType);

                result.Name.Should().Be("FakeClass");
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
