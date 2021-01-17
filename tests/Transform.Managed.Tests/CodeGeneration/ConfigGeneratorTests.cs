namespace Transform.Managed.Tests.CodeGeneration
{
    using System;
    using Autocrat.Transform.Managed.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using NSubstitute;
    using NSubstitute.ReturnsExtensions;
    using Xunit;

    // We're playing with statics, so prevent the nested test classes from
    // running in parallel
    [Collection(nameof(ConfigGeneratorTests))]
    public class ConfigGeneratorTests : IDisposable
    {
        private readonly JsonDeserializerBuilder builder;
        private readonly ConfigGenerator generator;

        private ConfigGeneratorTests()
        {
            this.builder = Substitute.For<JsonDeserializerBuilder>();
            ConfigGenerator.CreateBuilder = (_, __) => this.builder;

            this.builder.GenerateClass(null)
                .ReturnsForAnyArgs(new TypeDefinition("", "TestClass", TypeAttributes.Public));

            this.generator = new ConfigGenerator();
        }

        public void Dispose()
        {
            ConfigGenerator.CreateBuilder = null;
        }

        public sealed class GetClassForTests : ConfigGeneratorTests
        {
            [Fact]
            public void ShouldAddWritableProperties()
            {
                TypeReference classType = CodeHelper.CompileType(@"class TestClass
{
    public string ReadOnly { get; }
    public string ReadAndWrite { get; set; }
}");
                this.generator.GetClassFor(classType);

                this.builder.DidNotReceive().AddProperty(Arg.Is<PropertyDefinition>(p => p.Name == "ReadOnly"));
                this.builder.Received().AddProperty(Arg.Is<PropertyDefinition>(p => p.Name == "ReadAndWrite"));
            }

            [Fact]
            public void ShouldCacheClassTypes()
            {
                TypeReference classType = CodeHelper.CompileType(@"class TestClass
{
    public string Property { get; set; }
}");
                TypeDefinition result1 = this.generator.GetClassFor(classType);
                TypeDefinition result2 = this.generator.GetClassFor(classType);

                this.builder.ReceivedWithAnyArgs(1).AddProperty(null);
                result1.Should().BeSameAs(result2);
            }

            [Fact]
            public void ShouldCheckForCyclicProperties()
            {
                TypeReference cyclic = CodeHelper.CompileType(@"class Cyclic
{
    public Cyclic Parent { get; set; }
}");
                this.builder.GenerateClass(null)
                    .ReturnsNullForAnyArgs()
                    .AndDoes(ci => this.generator.GetClassFor(cyclic));

                this.generator.Invoking(g => g.GetClassFor(cyclic))
                    .Should().Throw<InvalidOperationException>()
                    .WithMessage("*cyclic*");
            }
        }
    }
}
