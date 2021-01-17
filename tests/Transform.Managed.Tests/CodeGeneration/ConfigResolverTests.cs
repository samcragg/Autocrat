namespace Transform.Managed.Tests.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Abstractions;
    using Autocrat.Transform.Managed;
    using Autocrat.Transform.Managed.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using NSubstitute;
    using Xunit;

    public class ConfigResolverTests
    {
        private const string MinimalConfigClass = @"
[Autocrat.Abstractions.Configuration]
class ConfigClass
{
}";

        private readonly Lazy<ConfigResolver> config;
        private readonly ConfigGenerator configGenerator = Substitute.For<ConfigGenerator>();
        private readonly KnownTypes knownTypes = Substitute.For<KnownTypes>();
        private readonly List<TypeDefinition> types = new List<TypeDefinition>();

        private ConfigResolverTests()
        {
            this.knownTypes.GetEnumerator().Returns(_ => this.types.GetEnumerator());
            this.config = new Lazy<ConfigResolver>(() => new ConfigResolver(this.knownTypes, this.configGenerator));
        }

        private ConfigResolver Config => this.config.Value;

        private TypeDefinition AddClass(string definition)
        {
            TypeDefinition configType = CodeHelper.CompileType(definition).Resolve();
            this.RegisterType(configType);
            return configType;
        }

        private void RegisterType(TypeDefinition type)
        {
            ModuleDefinition module = type.Module;
            this.types.Add(type);

            var deserializer = new TypeDefinition("", "Deserializer", default);
            deserializer.Methods.Add(new MethodDefinition(
                Constants.Constructor,
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName,
                module.TypeSystem.Void));

            deserializer.Methods.Add(new MethodDefinition(
                JsonDeserializerBuilder.ReadMethodName,
                default,
                module.TypeSystem.Void));

            this.configGenerator.GetClassFor(type)
                .ReturnsForAnyArgs(deserializer);
        }

        public sealed class ConstructorTests : ConfigResolverTests
        {
            [Fact]
            public void ShouldThrowForMultipleConfigurationClasses()
            {
                static TypeDefinition CreateConfigType(string name)
                {
                    System.Reflection.ConstructorInfo attributeConstructor =
                        typeof(ConfigurationAttribute).GetConstructor(Type.EmptyTypes);

                    var module = ModuleDefinition.CreateModule(name, ModuleKind.Console);
                    var definition = new TypeDefinition("", name, default);
                    definition.CustomAttributes.Add(new CustomAttribute(
                        module.ImportReference(attributeConstructor)));
                    return definition;
                }

                this.types.Add(CreateConfigType("ConfigType1"));
                this.types.Add(CreateConfigType("ConfigType2"));

                Action construct = () => new ConfigResolver(this.knownTypes, this.configGenerator);

                construct.Should().Throw<InvalidOperationException>();
            }
        }

        public sealed class EmitAccessConfigTests : ConfigResolverTests
        {
            [Fact]
            public void ShouldResolveNestedProperties()
            {
                ModuleDefinition module = CodeHelper.CompileCode(
                    referenceAbstractions: true,
                    code: @"
class OtherConfigClass
{
}

[Autocrat.Abstractions.Configuration]
class MainConfigClass
{
    public OtherConfigClass Section { get; set; }
}");

                TypeDefinition mainConfigClass = module.Types.Single(t => t.Name == "MainConfigClass");
                TypeDefinition otherConfigClass = module.Types.Single(t => t.Name == "OtherConfigClass");
                this.RegisterType(mainConfigClass);
                this.RegisterType(otherConfigClass);

                this.Config.EmitConfigurationClass(module);
                bool result = this.Config.EmitAccessConfig(
                    otherConfigClass,
                    GetProcessor(module));

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldResolveTheRootConfigurationClass()
            {
                TypeDefinition configType = this.AddClass(MinimalConfigClass);

                this.Config.EmitConfigurationClass(configType.Module);
                bool result = this.Config.EmitAccessConfig(
                    configType,
                    GetProcessor(configType.Module));

                result.Should().BeTrue();
            }

            private static ILProcessor GetProcessor(ModuleDefinition module)
            {
                var type = new TypeDefinition("", "TestType", default);
                module.Types.Add(type);

                var method = new MethodDefinition("TestMethod", default, module.TypeSystem.Void);
                type.Methods.Add(method);

                return method.Body.GetILProcessor();
            }
        }

        public sealed class EmitConfigurationClassTests : ConfigResolverTests
        {
            [Fact]
            public void ShouldHaveTheReadConfigurationMethod()
            {
                TypeDefinition configType = this.AddClass(MinimalConfigClass);

                TypeDefinition generated = this.Config.EmitConfigurationClass(configType.Module);

                generated.Methods.Should()
                    .ContainSingle(m => m.IsStatic && m.Name == ConfigResolver.ReadConfigurationMethod);
            }

            [Fact]
            public void ShouldReturnNullIfThereAreNoConfigClasses()
            {
                var module = ModuleDefinition.CreateModule("Test", ModuleKind.Console);
                TypeDefinition result = this.Config.EmitConfigurationClass(module);

                result.Should().BeNull();
            }
        }
    }
}
