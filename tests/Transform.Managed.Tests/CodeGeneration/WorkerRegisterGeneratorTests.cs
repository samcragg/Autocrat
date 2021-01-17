namespace Transform.Managed.Tests.CodeGeneration
{
    using System.Collections.Generic;
    using Autocrat.Transform.Managed.CodeGeneration;
    using Mono.Cecil;
    using NSubstitute;
    using Xunit;

    public class WorkerRegisterGeneratorTests
    {
        private readonly InstanceBuilder builder;
        private readonly WorkerRegisterGenerator generator;
        private readonly NativeImportGenerator nativeGenerator;
        private readonly List<TypeReference> workerTypes = new List<TypeReference>();

        private WorkerRegisterGeneratorTests()
        {
            this.builder = Substitute.For<InstanceBuilder>();
            this.nativeGenerator = Substitute.For<NativeImportGenerator>();

            this.generator = new WorkerRegisterGenerator(this.builder, this.workerTypes, this.nativeGenerator);
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

                this.nativeGenerator.ReceivedWithAnyArgs(3).RegisterMethod(null, null);
            }
        }
    }
}
