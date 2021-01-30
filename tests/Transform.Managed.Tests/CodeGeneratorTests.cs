namespace Transform.Managed.Tests
{
    using System.IO;
    using Autocrat.Transform.Managed;
    using Mono.Cecil;
    using NSubstitute;
    using Xunit;

    public class CodeGeneratorTests
    {
        private readonly ServiceFactory factory;
        private readonly CodeGenerator generator;
        private readonly ModuleDefinition module;

        private CodeGeneratorTests()
        {
            this.factory = Substitute.For<ServiceFactory>();
            this.module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
            this.generator = new CodeGenerator(this.factory, this.module);
        }

        public sealed class EmitAssemblyTests : CodeGeneratorTests
        {
            [Fact]
            public void ShouldGenerateTheCallbacks()
            {
                this.generator.EmitAssembly(Stream.Null, Stream.Null);

                this.factory.GetManagedCallbackGenerator()
                    .Received()
                    .EmitType(this.module);
            }

            [Fact]
            public void ShouldGenerateTheInitializers()
            {
                this.generator.EmitAssembly(Stream.Null, Stream.Null);

                this.factory.CreateInitializerGenerator()
                    .Received()
                    .Emit(this.module);
            }

            [Fact]
            public void ShouldRewriteTheModule()
            {
                this.generator.EmitAssembly(Stream.Null, Stream.Null);

                this.factory.CreateModuleRewriter()
                    .Received()
                    .Visit(this.module);
            }
        }
    }
}
