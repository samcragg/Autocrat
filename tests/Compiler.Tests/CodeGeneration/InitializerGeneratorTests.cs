namespace Compiler.Tests.CodeGeneration
{
    using Autocrat.Abstractions;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using NSubstitute;
    using Xunit;

    public class InitializerGeneratorTests
    {
        private readonly InstanceBuilder builder;
        private readonly InitializerGenerator initializer;

        private InitializerGeneratorTests()
        {
            this.builder = Substitute.For<InstanceBuilder>();
            this.initializer = new InitializerGenerator(this.builder);
        }

        private void EmitFor(string classDefinition)
        {
            TypeReference type = CodeHelper.CompileType(classDefinition);
            ModuleDefinition module = type.Module;
            this.initializer.AddClass(type.Resolve());
            this.initializer.Emit(module);
        }

        public sealed class EmitTests : InitializerGeneratorTests
        {
            [Fact]
            public void ShouldAddExplicitInterfaceMethods()
            {
                this.EmitFor(@"
using Autocrat.Abstractions;
class Explicit : IInitializer
{
    void IInitializer.OnConfigurationLoaded()
    {
    }
}");
                this.builder.Received().EmitNewObj(
                    Arg.Is<TypeReference>(t => t.Name == "Explicit"),
                    Arg.Any<ILProcessor>());
            }

            [Fact]
            public void ShouldAddImplicitInterfaceMethods()
            {
                this.EmitFor(@"
class Implicit : Autocrat.Abstractions.IInitializer
{
    public void OnConfigurationLoaded()
    {
    }
}");

                this.builder.Received().EmitNewObj(
                    Arg.Is<TypeReference>(t => t.Name == "Implicit"),
                    Arg.Any<ILProcessor>());
            }

            [Fact]
            public void ShouldExportTheOnConfigurationLoadedMethod()
            {
                var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
                this.initializer.Emit(module);

                TypeDefinition generatedType = module.Types.Should()
                    .ContainSingle(x => x.Name == InitializerGenerator.GeneratedClassName)
                    .Subject;

                CodeHelper.AssertHasExportedMember(
                    generatedType,
                    nameof(IInitializer.OnConfigurationLoaded));
            }
        }
    }
}
