namespace Transform.Managed.Tests.CodeGeneration
{
    using System.Collections.Generic;
    using Autocrat.Transform.Managed;
    using Autocrat.Transform.Managed.CodeGeneration;
    using Mono.Cecil;
    using NSubstitute;
    using Xunit;

    public class WorkerRegisterGeneratorTests
    {
        private readonly InstanceBuilder builder;
        private readonly WorkerRegisterGenerator generator;
        private readonly ExportedMethods exportedMethods;
        private readonly List<TypeReference> workerTypes = new List<TypeReference>();

        private WorkerRegisterGeneratorTests()
        {
            this.builder = Substitute.For<InstanceBuilder>();
            this.exportedMethods = Substitute.For<ExportedMethods>();

            this.generator = new WorkerRegisterGenerator(this.builder, this.workerTypes, this.exportedMethods);
        }

        public sealed class GenerateTests : WorkerRegisterGeneratorTests
        {
            [Fact]
            public void ShouldCreateAClassWithTheRegisterWorkerTypesMethod()
            {
                var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
                this.generator.EmitWorkerClass(module);

                CodeHelper.AssertHasExportedMember(
                    module.GetType(WorkerRegisterGenerator.GeneratedClassName),
                    WorkerRegisterGenerator.GeneratedMethodName);
            }

            [Fact]
            public void ShouldRegisterTheConstructorsAsNativeMethods()
            {
                TypeDefinition testWorker = CodeHelper.CompileType(@"class WorkerType { }");
                this.workerTypes.Add(testWorker);
                this.workerTypes.Add(testWorker);
                this.workerTypes.Add(testWorker);

                this.generator.EmitWorkerClass(testWorker.Module);

                this.exportedMethods.ReceivedWithAnyArgs(3).RegisterMethod(null, null);
            }
        }
    }
}
