namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class ConstructorResolverTests
    {
        private readonly INamedTypeSymbol abstractClass;
        private readonly INamedTypeSymbol arrayOfDependencies;
        private readonly ConfigResolver configResolver;
        private readonly INamedTypeSymbol defaultConstructor;
        private readonly InterfaceResolver interfaceResolver;
        private readonly IKnownTypes knownTypes;
        private readonly INamedTypeSymbol multipleConstructors;
        private readonly INamedTypeSymbol multipleDependencies;
        private readonly ConstructorResolver resolver;
        private readonly INamedTypeSymbol singleDependency;

        private ConstructorResolverTests()
        {
            Compilation compilation = CompilationHelper.CompileCode(@"
abstract class AbstractClass
{
}

class ArrayOfDependencies
{
    public ArrayOfDependencies(AbstractClass[] a)
    {
    }
}

class DefaultConstructor
{
}

class MultipleConstructors
{
    public MultipleConstructors(AbstractClass a)
    {
    }

    public MultipleConstructors(AbstractClass a, DefaultConstructor d)
    {
    }
}

class MultipleDependencies
{
    public MultipleDependencies(System.Collections.Generic.IEnumerable<AbstractClass> a)
    {
    }
}

class SingleDependency
{
    public SingleDependency(AbstractClass a)
    {
    }
}");
            this.abstractClass = compilation.GetTypeByMetadataName("AbstractClass");
            this.arrayOfDependencies = compilation.GetTypeByMetadataName("ArrayOfDependencies");
            this.defaultConstructor = compilation.GetTypeByMetadataName("DefaultConstructor");
            this.multipleConstructors = compilation.GetTypeByMetadataName("MultipleConstructors");
            this.multipleDependencies = compilation.GetTypeByMetadataName("MultipleDependencies");
            this.singleDependency = compilation.GetTypeByMetadataName("SingleDependency");

            this.configResolver = Substitute.For<ConfigResolver>();

            this.interfaceResolver = Substitute.For<InterfaceResolver>(Substitute.For<IKnownTypes>());
            this.interfaceResolver.FindClasses(null)
                .ReturnsForAnyArgs(ci => new[] { (INamedTypeSymbol)ci.Args()[0] });

            this.knownTypes = Substitute.For<IKnownTypes>();
            this.knownTypes.FindClassForInterface(null)
                .ReturnsForAnyArgs((ITypeSymbol)null);

            this.resolver = new ConstructorResolver(
                compilation,
                this.knownTypes,
                this.configResolver,
                this.interfaceResolver);
        }

        public sealed class GetParametersTests : ConstructorResolverTests
        {
            [Fact]
            public void ShouldReturnAnEmptyArrayForDefaultConstructors()
            {
                IReadOnlyList<object> result = this.resolver.GetParameters(
                    this.defaultConstructor);

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnArrayCompatibleDependencies()
            {
                IReadOnlyList<object> result = this.resolver.GetParameters(
                    this.multipleDependencies);

                this.interfaceResolver.DidNotReceiveWithAnyArgs().FindClasses(null);
                result.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<IArrayTypeSymbol>()
                    .Which.ElementType.Should().Be(this.abstractClass);
            }

            [Fact]
            public void ShouldReturnArrayDependencies()
            {
                IReadOnlyList<object> result = this.resolver.GetParameters(
                    this.arrayOfDependencies);

                this.interfaceResolver.DidNotReceiveWithAnyArgs().FindClasses(null);
                result.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<IArrayTypeSymbol>()
                    .Which.ElementType.Should().Be(this.abstractClass);
            }

            [Fact]
            public void ShouldReturnConfigurationTypes()
            {
                ExpressionSyntax expression = SyntaxFactory.ParseExpression("default");
                this.configResolver.AccessConfig(this.abstractClass)
                    .Returns(expression);

                IReadOnlyList<object> result = this.resolver.GetParameters(
                    this.singleDependency);

                result.Should().ContainSingle().Which.Should().BeSameAs(expression);
            }

            [Fact]
            public void ShouldReturnDependencyTypes()
            {
                IReadOnlyList<object> result = this.resolver.GetParameters(
                    this.singleDependency);

                result.Should().ContainSingle().Which.Should().Be(this.abstractClass);
            }

            [Fact]
            public void ShouldReturnNullExpressionForRewrittenInterfaces()
            {
                this.knownTypes.FindClassForInterface(this.abstractClass)
                    .Returns(this.abstractClass);

                IReadOnlyList<object> result = this.resolver.GetParameters(
                    this.singleDependency);

                result.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<ExpressionSyntax>()
                    .Which.ToFullString().Should().Be("null");
            }

            [Fact]
            public void ShouldReturnTheConstructorWithTheMostParameters()
            {
                IReadOnlyList<object> result = this.resolver.GetParameters(
                    this.multipleConstructors);

                result.Should().HaveCount(2);
                result.Should().HaveElementAt(0, this.abstractClass);
                result.Should().HaveElementAt(1, this.defaultConstructor);
            }

            [Fact]
            public void ShouldThrowIfMultipleDependenciesAreFound()
            {
                this.interfaceResolver.FindClasses(this.abstractClass)
                    .Returns(new INamedTypeSymbol[2]);

                this.resolver.Invoking(x => x.GetParameters(this.singleDependency))
                    .Should().Throw<InvalidOperationException>()
                    .WithMessage("Multiple*");
            }

            [Fact]
            public void ShouldThrowIfNoDependenciesAreFound()
            {
                this.interfaceResolver.FindClasses(this.abstractClass)
                    .Returns(Array.Empty<INamedTypeSymbol>());

                this.resolver.Invoking(x => x.GetParameters(this.singleDependency))
                    .Should().Throw<InvalidOperationException>()
                    .WithMessage("Unable to find*");
            }
        }
    }
}
