namespace Compiler.Tests
{
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Mono.Cecil;
    using Xunit;

    public class KnownTypesTests
    {
        private readonly KnownTypes knownTypes = new KnownTypes();

        public sealed class FindClassForInterfaceTests : KnownTypesTests
        {
            [Fact]
            public void ShouldExtractRewrittenInterfaces()
            {
                ModuleDefinition module = CodeHelper.CompileCode(@"
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

                this.knownTypes.Scan(module);
                TypeDefinition result = this.knownTypes.FindClassForInterface(
                    module.GetType("TestNamespace.IFakeInterface"));

                result.FullName.Should().Be("TestNamespace.FakeClass");
            }
        }

        public sealed class GetEnumeratorTests : KnownTypesTests
        {
            [Fact]
            [SuppressMessage("FluentAssertionTips", "CollectionShouldHaveCount:Simplify Assertion", Justification = "We want to test the Count property")]
            public void NonGenericGetEnumeratorShouldReturnAllTheItems()
            {
                ModuleDefinition module = CodeHelper.CompileCode(@"
public class Class1 { }
public class Class2 { }
");

                this.knownTypes.Scan(module);
                IEnumerator enumerator = ((IEnumerable)this.knownTypes).GetEnumerator();

                this.knownTypes.Count.Should().Be(2);
                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeFalse();
            }
        }

        public sealed class ScanTests : KnownTypesTests
        {
            [Fact]
            public void ShouldExtractDeclaredClasses()
            {
                ModuleDefinition module = CodeHelper.CompileCode(@"
namespace TestNamespace
{
    public class ExampleClass
    {
    }
}");
                this.knownTypes.Scan(module);

                this.knownTypes.Should().ContainSingle()
                    .Which.FullName.Should().Be("TestNamespace.ExampleClass");
            }

            [Fact]
            public void ShouldExtractDeclaredStructs()
            {
                ModuleDefinition module = CodeHelper.CompileCode(@"
namespace TestNamespace
{
    public struct ExampleStruct
    {
    }
}");
                this.knownTypes.Scan(module);

                this.knownTypes.Should().ContainSingle()
                    .Which.FullName.Should().Be("TestNamespace.ExampleStruct");
            }

            [Fact]
            public void ShouldExtractTypesInNestedNamespaces()
            {
                ModuleDefinition module = CodeHelper.CompileCode(@"
namespace OuterNamespace.InnerNamespace
{
    public class ExampleClass
    {
    }
}");
                this.knownTypes.Scan(module);

                this.knownTypes.Should().ContainSingle()
                    .Which.FullName.Should().Be("OuterNamespace.InnerNamespace.ExampleClass");
            }
        }
    }
}
