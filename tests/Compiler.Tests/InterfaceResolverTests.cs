namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Xunit;

    public class InterfaceResolverTests
    {
        private readonly InterfaceResolver resolver = new InterfaceResolver();

        private interface IFakeInterface
        {
        }

        public class FindClassesTests : InterfaceResolverTests
        {
            [Fact]
            public void ShouldNotReturnAbstractClasses()
            {
                this.resolver.AddKnownClasses(new[] { typeof(AbstractClass) });

                IReadOnlyList<Type> result = this.resolver.FindClasses(
                    typeof(IFakeInterface));

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnAllClassesImplementingAnInterface()
            {
                Type[] classes = new[]
                {
                    typeof(FakeClass1),
                    typeof(FakeClass2)
                };

                this.resolver.AddKnownClasses(classes);

                IReadOnlyList<Type> result = this.resolver.FindClasses(
                    typeof(IFakeInterface));

                result.Should().BeEquivalentTo(classes);
            }

            [Fact]
            public void ShouldReturnAnEmptyListForUnknownInterfaces()
            {
                IReadOnlyList<Type> result = this.resolver.FindClasses(
                    typeof(IFakeInterface));

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnClassesForBaseInterfaces()
            {
                this.resolver.AddKnownClasses(new[] { typeof(DerivedClass) });

                IReadOnlyList<Type> result = this.resolver.FindClasses(
                    typeof(IFakeInterface));

                result.Should().ContainSingle().Which.Should().Be(typeof(DerivedClass));
            }
        }

        private abstract class AbstractClass : IFakeInterface
        {
        }

        private class DerivedClass : AbstractClass
        {
        }

        private class FakeClass1 : IFakeInterface
        {
        }

        private class FakeClass2 : IFakeInterface
        {
        }
    }
}
