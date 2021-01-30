namespace Transform.Managed.Tests
{
    using System;
    using Autocrat.Common;
    using Autocrat.Transform.Managed;
    using Mono.Cecil;
    using NSubstitute;
    using Xunit;

    public class ManagedCompilerTests
    {
        private readonly IOutputFile assembly;
        private readonly AssemblyDefinition assemblyDefinition;
        private readonly ManagedCompiler compiler;
        private readonly IOutputFile exports;
        private readonly ServiceFactory factory;
        private readonly IOutputFile pdb;

        private ManagedCompilerTests()
        {
            this.assemblyDefinition = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("Test", new Version(0, 1)),
                "Module",
                ModuleKind.Dll);

            this.assembly = Substitute.For<IOutputFile>();
            this.exports = Substitute.For<IOutputFile>();
            this.pdb = Substitute.For<IOutputFile>();

            this.factory = Substitute.For<ServiceFactory>();
            this.factory.CreateAssemblyLoader().Load(null)
                .ReturnsForAnyArgs(assemblyDefinition);

            this.compiler = new ManagedCompiler(
                "",
                this.assembly,
                this.pdb,
                this.exports,
                this.factory);
        }

        public sealed class TransformTests : ManagedCompilerTests
        {
            [Fact]
            public void ShouldSaveTheExportedMethods()
            {
                this.compiler.Transform();

                this.factory.GetExportedMethods().Received()
                    .SerializeTo(this.exports.Stream);
            }
        }
    }
}
