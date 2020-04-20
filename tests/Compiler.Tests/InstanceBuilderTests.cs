namespace Compiler.Tests
{
    using System;
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class InstanceBuilderTests
    {
        private const string ClassDeclarations = @"
public class Dependency
{
    public Dependency(object injected)
    {
        this.Injected = injected;
    }

    public object Injected { get; }
}

public class DerivedClass : SimpleClass
{
}

public class SimpleClass
{
}";

        private readonly InstanceBuilder builder;
        private readonly Compilation compilation;
        private readonly ConstructorResolver constructorResolver;
        private readonly INamedTypeSymbol dependency;
        private readonly INamedTypeSymbol derivedClass;
        private readonly InterfaceResolver interfaceResolver;
        private readonly INamedTypeSymbol simpleClass;

        private InstanceBuilderTests()
        {
            this.compilation = CompilationHelper.CompileCode(ClassDeclarations);
            this.dependency = this.compilation.GetTypeByMetadataName("Dependency");
            this.derivedClass = this.compilation.GetTypeByMetadataName("DerivedClass");
            this.simpleClass = this.compilation.GetTypeByMetadataName("SimpleClass");

            this.constructorResolver = Substitute.For<ConstructorResolver>(new object[] { null, null, null });
            this.interfaceResolver = Substitute.For<InterfaceResolver>(Substitute.For<IKnownTypes>());
            this.builder = new InstanceBuilder(this.constructorResolver, this.interfaceResolver);
        }

        private object GetLocal(IdentifierNameSyntax identifier)
        {
            string locals = string.Join(
                Environment.NewLine,
                this.builder.LocalDeclarations.Select(x => x.NormalizeWhitespace()));

            string code = ClassDeclarations + @"
public static class WrapperClass
{
    public static object WrapperMethod()
    {
        " + locals + @"
        return " + identifier.ToString() + @";
    }
}";
            Type wrapperType = CompilationHelper.GetGeneratedType(
                CompilationHelper.CompileCode(code),
                "WrapperClass");
            return wrapperType.GetMethod("WrapperMethod").Invoke(null, null);
        }

        public sealed class GenerateTypeForTests : InstanceBuilderTests
        {
            [Fact]
            public void ShouldCheckForCyclicDependencies()
            {
                this.constructorResolver.GetParameters(this.simpleClass)
                    .Returns(new[] { this.simpleClass });

                this.builder.Invoking(x => x.GenerateForType(this.simpleClass))
                    .Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldCreateAnInstanceOfTheSpecifiedType()
            {
                IdentifierNameSyntax identifier = this.builder.GenerateForType(this.simpleClass);

                object instance = this.GetLocal(identifier);
                instance.GetType().Name.Should().Be(this.simpleClass.Name);
            }

            [Fact]
            public void ShouldInjectArrays()
            {
                this.constructorResolver.GetParameters(this.dependency)
                    .Returns(new[] { this.compilation.CreateArrayTypeSymbol(this.simpleClass) });

                this.interfaceResolver.FindClasses(this.simpleClass)
                    .Returns(new[] { this.simpleClass, this.derivedClass });

                IdentifierNameSyntax identifier = this.builder.GenerateForType(this.dependency);

                dynamic instance = this.GetLocal(identifier);
                ((int)instance.Injected.Length).Should().Be(2);
                ((object)instance.Injected[0]).Should().NotBeNull();
                ((object)instance.Injected[1]).Should().NotBeNull();
                ((object)instance.Injected[0]).Should().NotBe(instance.Injected[1]);
            }

            [Fact]
            public void ShouldInjectDependencies()
            {
                this.constructorResolver.GetParameters(this.dependency)
                    .Returns(new[] { this.simpleClass });

                IdentifierNameSyntax identifier = this.builder.GenerateForType(this.dependency);

                dynamic instance = this.GetLocal(identifier);
                ((object)instance.Injected).GetType().Name.Should().Be(this.simpleClass.Name);
            }

            [Fact]
            public void ShouldInjectNulls()
            {
                this.constructorResolver.GetParameters(this.dependency)
                    .Returns(new ITypeSymbol[] { null });

                IdentifierNameSyntax identifier = this.builder.GenerateForType(this.dependency);

                dynamic instance = this.GetLocal(identifier);
                ((object)instance.Injected).Should().BeNull();
            }

            [Fact]
            public void ShouldReturnExistingInstances()
            {
                IdentifierNameSyntax first = this.builder.GenerateForType(this.simpleClass);
                IdentifierNameSyntax second = this.builder.GenerateForType(this.simpleClass);

                second.Should().BeSameAs(first);
            }
        }
    }
}
