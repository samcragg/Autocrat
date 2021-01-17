namespace Transform.Managed.Tests
{
    using System.IO;
    using System.Text;
    using Autocrat.Transform.Managed;
    using FluentAssertions;
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

        public sealed class EmitNativeCodeTests : CodeGeneratorTests
        {
            [Fact]
            public void ShouldSetTheDescription()
            {
                this.GetOutput(null, "Test description")
                    .Should().Contain("set_description(\"Test description\");");
            }

            [Fact]
            public void ShouldSetTheVersion()
            {
                this.GetOutput("1.2.3", null)
                    .Should().Contain("set_version(\"1.2.3\");");
            }

            [Fact]
            public void ShouldWriteTheNativeImportCode()
            {
                this.generator.EmitNativeCode(null, null, Stream.Null);

                this.factory.GetNativeImportGenerator()
                    .Received()
                    .WriteTo(Stream.Null);
            }

            private string GetOutput(string version, string description)
            {
                using var stream = new MemoryStream();
                this.generator.EmitNativeCode(version, description, stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
