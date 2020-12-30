namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Compiler;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using NSubstitute;
    using Xunit;

    public class ConstructorResolverTests
    {
        private readonly InterfaceResolver interfaceResolver;
        private readonly ModuleDefinition module;
        private readonly ConstructorResolver resolver;

        private ConstructorResolverTests()
        {
            this.module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Console);

            this.interfaceResolver = Substitute.For<InterfaceResolver>(Substitute.For<KnownTypes>());
            this.resolver = new ConstructorResolver(this.interfaceResolver);
        }

        private MethodDefinition CreateConstructor(params TypeReference[] parameters)
        {
            var ctor = new MethodDefinition(
                Constants.Constructor,
                Constants.PublicConstructor,
                this.module.TypeSystem.Void);

            foreach (TypeReference type in parameters)
            {
                ctor.Parameters.Add(new ParameterDefinition(type));
            }

            return ctor;
        }

        public sealed class GetConstructorTests : ConstructorResolverTests
        {
            [Fact]
            public void ShouldReturnTheConstructorWithTheMostParameters()
            {
                var dependencyType = new TypeDefinition("", "Dependency", default);
                var classType = new TypeDefinition("", "Class", default);
                MethodDefinition oneParameter = this.CreateConstructor(dependencyType);
                MethodDefinition twoParameters = this.CreateConstructor(dependencyType, dependencyType);
                classType.Methods.Add(oneParameter);
                classType.Methods.Add(twoParameters);

                MethodDefinition result = this.resolver.GetConstructor(classType);

                result.Should().BeSameAs(twoParameters);
            }
        }

        public sealed class GetParametersTests : ConstructorResolverTests
        {
            [Fact]
            public void ShouldReturnAnEmptyArrayForDefaultConstructors()
            {
                IEnumerable<TypeReference> result =
                    this.resolver.GetParameters(this.CreateConstructor());

                result.Should().BeEmpty();
            }

            // Types taken from https://docs.microsoft.com/en-gb/dotnet/api/system.array
            [Theory]
            [InlineData(typeof(IList<string>))]
            [InlineData(typeof(ICollection<string>))]
            [InlineData(typeof(IEnumerable<string>))]
            [InlineData(typeof(IReadOnlyList<string>))]
            [InlineData(typeof(IReadOnlyCollection<string>))]
            public void ShouldReturnArrayCompatibleDependencies(Type parameterType)
            {
                MethodDefinition constructor = this.CreateConstructor(
                    this.module.ImportReference(parameterType));

                IReadOnlyCollection<TypeReference> result =
                    this.resolver.GetParameters(constructor);

                this.interfaceResolver.DidNotReceiveWithAnyArgs().FindClasses(null);
                result.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<ArrayType>()
                    .Which.ElementType.FullName.Should().Be("System.String");
            }

            [Fact]
            public void ShouldReturnArrayDependencies()
            {
                MethodDefinition constructor = this.CreateConstructor(
                    new ArrayType(this.module.TypeSystem.String));

                IReadOnlyCollection<TypeReference> result =
                    this.resolver.GetParameters(constructor);

                this.interfaceResolver.DidNotReceiveWithAnyArgs().FindClasses(null);
                result.Should().ContainSingle()
                    .Which.Should().BeAssignableTo<ArrayType>()
                    .Which.ElementType.FullName.Should().Be("System.String");
            }

            [Fact]
            public void ShouldReturnDependencyTypes()
            {
                var dependencyType = new TypeDefinition("", "TestDependency", default);
                MethodDefinition constructor = this.CreateConstructor(dependencyType);

                this.interfaceResolver.FindClasses(dependencyType)
                    .Returns(new[] { dependencyType });

                IReadOnlyCollection<TypeReference> result =
                    this.resolver.GetParameters(constructor);

                result.Should().ContainSingle().Which.Should().Be(dependencyType);
            }

            [Fact]
            public void ShouldThrowIfMultipleDependenciesAreFound()
            {
                var dependencyType = new TypeDefinition("", "TestDependency", default);
                MethodDefinition constructor = this.CreateConstructor(dependencyType);

                this.interfaceResolver.FindClasses(dependencyType)
                    .Returns(new TypeDefinition[2]);

                this.resolver.Invoking(x => x.GetParameters(constructor))
                    .Should().Throw<InvalidOperationException>()
                    .WithMessage("Multiple*");
            }

            [Fact]
            public void ShouldThrowIfNoDependenciesAreFound()
            {
                var dependencyType = new TypeDefinition("", "TestDependency", default);
                MethodDefinition constructor = this.CreateConstructor(dependencyType);

                this.interfaceResolver.FindClasses(dependencyType)
                    .Returns(new TypeDefinition[0]);

                this.resolver.Invoking(x => x.GetParameters(constructor))
                    .Should().Throw<InvalidOperationException>()
                    .WithMessage("Unable to find*");
            }
        }
    }
}
