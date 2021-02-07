namespace Transform.Managed.Tests
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Transform.Managed;
    using FluentAssertions;
    using Mono.Cecil;
    using NSubstitute;
    using Xunit;

    public class InterfaceResolverTests
    {
        private readonly TypeDefinition abstractClass;
        private readonly TypeDefinition derivedClass;
        private readonly TypeDefinition fakeClass1;
        private readonly TypeDefinition fakeClass2;
        private readonly TypeDefinition fakeInterface;
        private readonly KnownTypes knownTypes;
        private readonly Lazy<InterfaceResolver> resolver;

        private InterfaceResolverTests()
        {
            ModuleDefinition module = CodeHelper.CompileCode(@"
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
            this.abstractClass = module.GetType("AbstractClass");
            this.derivedClass = module.GetType("DerivedClass");
            this.fakeClass1 = module.GetType("FakeClass1");
            this.fakeClass2 = module.GetType("FakeClass2");
            this.fakeInterface = module.GetType("IFakeInterface");
            this.knownTypes = Substitute.For<KnownTypes>();
            this.resolver = new Lazy<InterfaceResolver>(() => new InterfaceResolver(this.knownTypes));
        }

        private InterfaceResolver Resolver => this.resolver.Value;

        private void SetKnownTypes(params TypeDefinition[] types)
        {
            this.knownTypes.GetEnumerator()
                .Returns(((IEnumerable<TypeDefinition>)types).GetEnumerator());
        }

        public class FindClassesTests : InterfaceResolverTests
        {
            [Fact]
            public void ShouldNotReturnAbstractClasses()
            {
                this.SetKnownTypes(this.abstractClass);

                IReadOnlyCollection<TypeDefinition> result = this.Resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnAllClassesImplementingAnInterface()
            {
                TypeDefinition[] classes = new[]
                {
                    this.fakeClass1,
                    this.fakeClass2,
                };

                this.SetKnownTypes(classes);

                IReadOnlyCollection<TypeDefinition> result = this.Resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEquivalentTo(classes);
            }

            [Fact]
            public void ShouldReturnAnEmptyListForUnknownInterfaces()
            {
                IReadOnlyCollection<TypeDefinition> result = this.Resolver.FindClasses(
                    this.fakeInterface);

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnClassesForAbstractBaseClasses()
            {
                this.SetKnownTypes(this.derivedClass);

                IReadOnlyCollection<TypeDefinition> result = this.Resolver.FindClasses(
                    this.abstractClass);

                result.Should().ContainSingle().Which.Should().Be(this.derivedClass);
            }

            [Fact]
            public void ShouldReturnClassesForBaseInterfaces()
            {
                this.SetKnownTypes(this.derivedClass);

                IReadOnlyCollection<TypeDefinition> result = this.Resolver.FindClasses(
                    this.fakeInterface);

                result.Should().ContainSingle().Which.Should().Be(this.derivedClass);
            }

            [Fact]
            public void ShouldReturnConcreteClasses()
            {
                this.SetKnownTypes(this.fakeClass1);

                IReadOnlyCollection<TypeDefinition> result = this.Resolver.FindClasses(
                    this.fakeClass1);

                result.Should().ContainSingle().Which.Should().Be(this.fakeClass1);
            }
        }
    }
}
