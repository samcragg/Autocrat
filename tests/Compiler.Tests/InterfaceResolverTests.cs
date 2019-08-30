namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using NSubstitute;
    using Xunit;

    public class InterfaceResolverTests
    {
        private readonly INamedTypeSymbol abstractClass;
        private readonly INamedTypeSymbol derivedClass;
        private readonly INamedTypeSymbol fakeClass1;
        private readonly INamedTypeSymbol fakeClass2;
        private readonly INamedTypeSymbol fakeInterface;
        private readonly IKnownTypes knownTypes;
        private readonly Lazy<InterfaceResolver> resolver;

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
            this.knownTypes = Substitute.For<IKnownTypes>();
            this.resolver = new Lazy<InterfaceResolver>(() => new InterfaceResolver(this.knownTypes));
        }

        private InterfaceResolver Resolver => this.resolver.Value;

        private void SetKnownTypes(params INamedTypeSymbol[] types)
        {
            this.knownTypes.GetEnumerator()
                .Returns(((IEnumerable<INamedTypeSymbol>)types).GetEnumerator());
        }

        public class FindClassesTests : InterfaceResolverTests
        {
            [Fact]
            public void ShouldNotReturnAbstractClasses()
            {
                this.SetKnownTypes(this.abstractClass);

                IReadOnlyCollection<INamedTypeSymbol> result = this.Resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnAllClassesImplementingAnInterface()
            {
                INamedTypeSymbol[] classes = new[]
                {
                    this.fakeClass1,
                    this.fakeClass2,
                };

                this.SetKnownTypes(classes);

                IReadOnlyCollection<INamedTypeSymbol> result = this.Resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEquivalentTo(classes);
            }

            [Fact]
            public void ShouldReturnAnEmptyListForUnknownInterfaces()
            {
                IReadOnlyCollection<INamedTypeSymbol> result = this.Resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnClassesForAbstractBaseClasses()
            {
                this.SetKnownTypes(this.derivedClass);

                IReadOnlyCollection<INamedTypeSymbol> result = this.Resolver.FindClasses(
                    this.abstractClass);

                result.Should().ContainSingle().Which.Should().Be(this.derivedClass);
            }

            [Fact]
            public void ShouldReturnClassesForBaseInterfaces()
            {
                this.SetKnownTypes(this.derivedClass);

                IReadOnlyCollection<INamedTypeSymbol> result = this.Resolver.FindClasses(
                    this.fakeInterface);

                result.Should().ContainSingle().Which.Should().Be(this.derivedClass);
            }

            [Fact]
            public void ShouldReturnConcreteClasses()
            {
                this.SetKnownTypes(this.fakeClass1);

                IReadOnlyCollection<INamedTypeSymbol> result = this.Resolver.FindClasses(
                    this.fakeClass1);

                result.Should().ContainSingle().Which.Should().Be(this.fakeClass1);
            }
        }
    }
}
