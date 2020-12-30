namespace Compiler.Tests.CodeGeneration
{
    using System;
    using Autocrat.Compiler.CodeGeneration;
    using Autocrat.NativeAdapters;
    using FluentAssertions;
    using Mono.Cecil;
    using Xunit;
    using SR = System.Reflection;

    public class ManagedExportsGeneratorTests
    {
        private readonly ManagedExportsGenerator generator = new ManagedExportsGenerator();

        public sealed class EmitTests : ManagedExportsGeneratorTests
        {
            [Fact]
            public void ShouldCreateAClassWithTheRegisterManagedTypesMethod()
            {
                var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
                this.generator.Emit(module);

                CodeHelper.AssertHasExportedMember(
                    module.GetType(ManagedExportsGenerator.GeneratedClassName),
                    ManagedExportsGenerator.GeneratedMethodName);
            }

            [Fact]
            public void ShouldInitializeTheConfigService()
            {
                TypeDefinition configClass = CodeHelper.CompileType(@"public static class FakeAppConfig
{
    public static int CallCount;

    public static void ReadConfig(ref System.Text.Json.Utf8JsonReader reader)
    {
        CallCount++;
    }
}");
                this.generator.ConfigClass = configClass;
                this.generator.Emit(configClass.Module);

                SR.Assembly assembly = CodeHelper.LoadModule(configClass.Module);
                InvokeRegisterManagedTypes(assembly);

                ConfigService.Load(new byte[0]);
                Type configType = assembly.GetType("FakeAppConfig");
                configType.GetField("CallCount").GetValue(null).Should().Be(1);
            }

            [Fact]
            public void ShouldRegisterTheWorkerTypes()
            {
                TypeDefinition workersClass = CodeHelper.CompileType(@"public static class FakeWorkers
{
    public static int CallCount;

    public static void RegisterWorkerTypes()
    {
        CallCount++;
    }
}");

                this.generator.WorkersClass = workersClass;
                this.generator.Emit(workersClass.Module);

                SR.Assembly assembly = CodeHelper.LoadModule(workersClass.Module);
                InvokeRegisterManagedTypes(assembly);

                Type workerType = assembly.GetType("FakeWorkers");
                workerType.GetField("CallCount").GetValue(null).Should().Be(1);
            }

            private static void InvokeRegisterManagedTypes(SR.Assembly assembly)
            {
                Type generatedType = assembly.GetType(ManagedExportsGenerator.GeneratedClassName);
                generatedType.GetMethod(ManagedExportsGenerator.GeneratedMethodName)
                             .Invoke(null, null);
            }
        }
    }
}
