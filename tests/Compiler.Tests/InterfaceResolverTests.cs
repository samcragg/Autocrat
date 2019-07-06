namespace Compiler.Tests
{
    using System.Collections.Generic;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Xunit;

    public class InterfaceResolverTests
    {
        private readonly ITypeSymbol abstractClass;
        private readonly ITypeSymbol derivedClass;
        private readonly ITypeSymbol fakeClass1;
        private readonly ITypeSymbol fakeClass2;
        private readonly ITypeSymbol fakeInterface;
        private readonly InterfaceResolver resolver;

        private InterfaceResolverTests()
        {
            Compilation compilation = CompilationHelper.CompileCode(@"
interface IFakeInterface
{
}

abstract class AbstractClass : IFakeInterface
{
}

class DerivedClass : AbstractClass
{
}

class FakeClass1 : IFakeInterface
{
}

class FakeClass2 : IFakeInterface
{
}");
            this.abstractClass = compilation.GetTypeByMetadataName("AbstractClass");
            this.derivedClass = compilation.GetTypeByMetadataName("DerivedClass");
            this.fakeClass1 = compilation.GetTypeByMetadataName("FakeClass1");
            this.fakeClass2 = compilation.GetTypeByMetadataName("FakeClass2");
            this.fakeInterface = compilation.GetTypeByMetadataName("IFakeInterface");
            this.resolver = new InterfaceResolver();
        }

        public class FindClassesTests : InterfaceResolverTests
        {
            [Fact]
            public void ShouldNotReturnAbstractClasses()
            {
                this.resolver.AddKnownClasses(new[] { this.abstractClass });

                IReadOnlyCollection<INamedTypeSymbol> result = this.resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnAllClassesImplementingAnInterface()
            {
                ITypeSymbol[] classes = new[]
                {
                    this.fakeClass1,
                    this.fakeClass2,
                };

                this.resolver.AddKnownClasses(classes);

                IReadOnlyCollection<INamedTypeSymbol> result = this.resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEquivalentTo(classes);
            }

            [Fact]
            public void ShouldReturnAnEmptyListForUnknownInterfaces()
            {
                IReadOnlyCollection<INamedTypeSymbol> result = this.resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnClassesForAbstractBaseClasses()
            {
                this.resolver.AddKnownClasses(new[] { this.derivedClass });

                IReadOnlyCollection<INamedTypeSymbol> result = this.resolver.FindClasses(
                    this.abstractClass);

                result.Should().ContainSingle().Which.Should().Be(this.derivedClass);
            }

            [Fact]
            public void ShouldReturnClassesForBaseInterfaces()
            {
                this.resolver.AddKnownClasses(new[] { this.derivedClass });

                IReadOnlyCollection<INamedTypeSymbol> result = this.resolver.FindClasses(
                    this.fakeInterface);

                result.Should().ContainSingle().Which.Should().Be(this.derivedClass);
            }

            [Fact]
            public void ShouldReturnConcreteClasses()
            {
                this.resolver.AddKnownClasses(new[] { this.fakeClass1 });

                IReadOnlyCollection<INamedTypeSymbol> result = this.resolver.FindClasses(
                    this.fakeClass1);

                result.Should().ContainSingle().Which.Should().Be(this.fakeClass1);
            }
        }
    }
}
