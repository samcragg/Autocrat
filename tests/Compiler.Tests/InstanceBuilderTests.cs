namespace Compiler.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Emit;
    using NSubstitute;
    using Xunit;

    public class InstanceBuilderTests
    {
        private readonly InstanceBuilder builder;
        private readonly ConstructorResolver constructorResolver;
        private readonly InterfaceResolver interfaceResolver;

        private InstanceBuilderTests()
        {
            this.constructorResolver = Substitute.For<ConstructorResolver>(new object[] { null });
            this.interfaceResolver = Substitute.For<InterfaceResolver>();
            this.builder = new InstanceBuilder(this.constructorResolver, this.interfaceResolver);
        }

        private object GetLocal(IdentifierNameSyntax identifier)
        {
            string locals = string.Join(
                Environment.NewLine,
                this.builder.LocalDeclarations.Select(x => x.NormalizeWhitespace()));

            string code = @"public static class WrapperClass
{
    public static object WrapperMethod()
    {
        " + locals + @"
        return " + identifier.ToString() + @";
    }
}";
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            MetadataReference[] references =
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(InstanceBuilderTests).Assembly.Location),
            };
            CSharpCompilation compilation = CSharpCompilation
                .Create("TestAssembly", options: options)
                .AddReferences(references)
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(code));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                result.Success.Should().BeTrue();

                Type wrapperType = Assembly.Load(ms.ToArray()).GetType("WrapperClass");
                return wrapperType.GetMethod("WrapperMethod").Invoke(null, null);
            }
        }

        public sealed class GenerateTypeForTests : InstanceBuilderTests
        {
            [Fact]
            public void ShouldCheckForCyclicDependencies()
            {
                this.constructorResolver.GetParameters(typeof(SimpleClass))
                    .Returns(new[] { typeof(SimpleClass) });

                this.builder.Invoking(x => x.GenerateForType(typeof(SimpleClass)))
                    .Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldCreateAnInstanceOfTheSpecifiedType()
            {
                IdentifierNameSyntax identifier = this.builder.GenerateForType(typeof(SimpleClass));

                object instance = this.GetLocal(identifier);
                instance.Should().BeOfType<SimpleClass>();
            }

            [Fact]
            public void ShouldInjectArrays()
            {
                this.constructorResolver.GetParameters(typeof(Dependency))
                    .Returns(new[] { typeof(SimpleClass[]) });

                this.interfaceResolver.FindClasses(typeof(SimpleClass))
                    .Returns(new[] { typeof(SimpleClass), typeof(DerivedClass) });

                IdentifierNameSyntax identifier = this.builder.GenerateForType(typeof(Dependency));

                object instance = this.GetLocal(identifier);
                instance.Should().BeOfType<Dependency>()
                    .Which.Injected.Should().BeOfType<SimpleClass[]>()
                    .Which.Length.Should().Be(2);
            }

            [Fact]
            public void ShouldInjectDependencies()
            {
                this.constructorResolver.GetParameters(typeof(Dependency))
                    .Returns(new[] { typeof(SimpleClass) });

                IdentifierNameSyntax identifier = this.builder.GenerateForType(typeof(Dependency));

                object instance = this.GetLocal(identifier);
                instance.Should().BeOfType<Dependency>()
                    .Which.Injected.Should().BeOfType<SimpleClass>();
            }

            [Fact]
            public void ShouldReturnExistingInstances()
            {
                IdentifierNameSyntax first = this.builder.GenerateForType(typeof(SimpleClass));
                IdentifierNameSyntax second = this.builder.GenerateForType(typeof(SimpleClass));

                second.Should().BeSameAs(first);
            }

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
            }
        }
    }
}
