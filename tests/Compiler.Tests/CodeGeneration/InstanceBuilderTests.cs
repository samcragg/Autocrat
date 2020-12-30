namespace Compiler.Tests.CodeGeneration
{
    using System;
    using System.Linq;
    using Autocrat.Compiler;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using NSubstitute;
    using Xunit;

    public class InstanceBuilderTests
    {
        private readonly InstanceBuilder builder;
        private readonly ConfigResolver configResolver;
        private readonly ConstructorResolver constructorResolver;
        private readonly InterfaceResolver interfaceResolver;

        private InstanceBuilderTests()
        {
            this.configResolver = Substitute.For<ConfigResolver>();
            this.interfaceResolver = Substitute.For<InterfaceResolver>();

            this.constructorResolver = Substitute.For<ConstructorResolver>();
            this.constructorResolver
                .WhenForAnyArgs(x => x.GetConstructor(null)).CallBase();

            this.builder = new InstanceBuilder(
                this.configResolver,
                this.constructorResolver,
                this.interfaceResolver);
        }

        private dynamic EmitAndCreate(TypeReference type)
        {
            var method = new CodeHelper.GeneratedMethod(type.Module);
            this.builder.EmitNewObj(type, method.IL);
            return method.GetResult();
        }

        public sealed class EmitNewObjTests : InstanceBuilderTests
        {
            [Fact]
            public void ShouldCheckForCyclicDependencies()
            {
                var classType = new TypeDefinition("", "CyclicClass", default);
                this.constructorResolver.GetParameters(null)
                    .ReturnsForAnyArgs(new[] { classType });

                var method = new CodeHelper.GeneratedMethod();
                this.builder.Invoking(x => x.EmitNewObj(classType, method.IL))
                    .Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldCreateAnInstanceOfTheSpecifiedType()
            {
                TypeReference simpleClass = CodeHelper.CompileType("class SimpleClass {}");

                object result = this.EmitAndCreate(simpleClass);

                result.GetType().Name.Should().Be("SimpleClass");
            }

            [Fact]
            public void ShouldInjectArrays()
            {
                TypeReference arrayDependency = CodeHelper.CompileType(@"
public class ArrayDependency
{
    public class Dependency
    {
    }

    public ArrayDependency(Dependency[] injected)
    {
        this.Injected = injected;
    }

    public Dependency[] Injected { get; }
}");
                TypeDefinition dependencyType = arrayDependency.Resolve()
                    .NestedTypes.Single()
                    .Resolve();

                this.constructorResolver.GetParameters(Arg.Is<MethodDefinition>(m => m.DeclaringType == arrayDependency))
                    .Returns(new[] { new ArrayType(dependencyType) });

                this.interfaceResolver.FindClasses(dependencyType)
                    .Returns(new[] { dependencyType, dependencyType });

                dynamic result = this.EmitAndCreate(arrayDependency);

                ((int)result.Injected.Length).Should().Be(2);
                ((object)result.Injected[0]).Should().NotBeNull();
                ((object)result.Injected[1]).Should().NotBeNull();
                ((object)result.Injected[0]).Should().NotBeSameAs(result.Injected[1]);
            }

            [Fact]
            public void ShouldInjectConfiguration()
            {
                var configType = new TypeDefinition("", "ConfigType", default);
                TypeReference injectedValue = CodeHelper.CompileType(@"
public class InjectedValue
{
    public InjectedValue(int value)
    {
        this.Value = value;
    }

    public int Value { get; }
}");

                this.constructorResolver.GetParameters(null)
                    .ReturnsForAnyArgs(new[] { configType });

                this.configResolver.EmitAccessConfig(configType, Arg.Any<ILProcessor>())
                    .Returns(ci =>
                    {
                        ci.Arg<ILProcessor>().Emit(OpCodes.Ldc_I4, 123);
                        return true;
                    });

                dynamic result = this.EmitAndCreate(injectedValue);

                ((int)result.Value).Should().Be(123);
            }

            [Fact]
            public void ShouldInjectTransientDependencies()
            {
                TypeReference multipleDependencies = CodeHelper.CompileType(@"
public class MultipleDependencies
{
    public class Dependency
    {
    }

    public MultipleDependencies(Dependency a, Dependency b)
    {
        this.A = a;
        this.B = b;
    }

    public Dependency A { get; }
    public Dependency B { get; }
}");
                TypeDefinition dependencyType = multipleDependencies.Resolve()
                    .NestedTypes.Single()
                    .Resolve();

                this.constructorResolver.GetParameters(Arg.Is<MethodDefinition>(m => m.DeclaringType == multipleDependencies))
                    .Returns(new[] { dependencyType, dependencyType });

                this.interfaceResolver.FindClasses(dependencyType)
                    .Returns(new[] { dependencyType, dependencyType });

                dynamic result = this.EmitAndCreate(multipleDependencies);

                ((object)result.A).Should().NotBeNull();
                ((object)result.B).Should().NotBeNull();
                ((object)result.A).Should().NotBeSameAs(result.B);
            }
        }
    }
}
