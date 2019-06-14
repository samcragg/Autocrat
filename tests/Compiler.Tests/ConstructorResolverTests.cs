namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Compiler;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ConstructorResolverTests
    {
        private readonly InterfaceResolver interfaceResolver;
        private readonly ConstructorResolver resolver;

        private ConstructorResolverTests()
        {
            this.interfaceResolver = Substitute.For<InterfaceResolver>();
            this.interfaceResolver.FindClasses(null)
                .ReturnsForAnyArgs(ci => new[] { ci.Arg<Type>() });

            this.resolver = new ConstructorResolver(this.interfaceResolver);
        }

        public sealed class GetParametersTests : ConstructorResolverTests
        {
            [Fact]
            public void ShouldReturnAnEmptyArrayForDefaultConstructors()
            {
                Type[] result = this.resolver.GetParameters(typeof(DefaultConstructor));

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnArrayDependencies()
            {
                Type[] result = this.resolver.GetParameters(typeof(ArrayOfDependencies));

                result.Should().ContainSingle().Which.Should().Be(typeof(AbstractClass[]));
                this.interfaceResolver.DidNotReceiveWithAnyArgs().FindClasses(null);
            }

            [Fact]
            public void ShouldReturnArrayCompatibleDependencies()
            {
                Type[] result = this.resolver.GetParameters(typeof(MultipleDependencies));

                result.Should().ContainSingle().Which.Should().Be(typeof(AbstractClass[]));
                this.interfaceResolver.DidNotReceiveWithAnyArgs().FindClasses(null);
            }

            [Fact]
            public void ShouldReturnDependencyTypes()
            {
                Type[] result = this.resolver.GetParameters(typeof(SingleDependency));

                result.Should().ContainSingle().Which.Should().Be(typeof(AbstractClass));
            }

            [Fact]
            public void ShouldReturnTheConstructorWithTheMostParameters()
            {
                Type[] result = this.resolver.GetParameters(typeof(MultipleConstructors));

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, typeof(AbstractClass));
                result.Should().HaveElementAt(1, typeof(DefaultConstructor));
            }

            [Fact]
            public void ShouldThrowIfMultipleDependenciesAreFound()
            {
                this.interfaceResolver.FindClasses(typeof(AbstractClass))
                    .Returns(new Type[2]);

                this.resolver.Invoking(x => x.GetParameters(typeof(SingleDependency)))
                    .Should().Throw<InvalidOperationException>()
                    .WithMessage("Multiple*");
            }

            [Fact]
            public void ShouldThrowIfNoDependenciesAreFound()
            {
                this.interfaceResolver.FindClasses(typeof(AbstractClass))
                    .Returns(Array.Empty<Type>());

                this.resolver.Invoking(x => x.GetParameters(typeof(SingleDependency)))
                    .Should().Throw<InvalidOperationException>()
                    .WithMessage("Unable to find*");
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private abstract class AbstractClass
        {
        }

        private class ArrayOfDependencies
        {
            public ArrayOfDependencies(AbstractClass[] a)
            {
            }
        }

        private class DefaultConstructor
        {
        }

        private class MultipleConstructors
        {
            public MultipleConstructors(AbstractClass a)
            {
            }

            public MultipleConstructors(AbstractClass a, DefaultConstructor d)
            {
            }
        }

        private class MultipleDependencies
        {
            public MultipleDependencies(IEnumerable<AbstractClass> a)
            {
            }
        }

        private class SingleDependency
        {
            public SingleDependency(AbstractClass a)
            {
            }
        }
    }
}
