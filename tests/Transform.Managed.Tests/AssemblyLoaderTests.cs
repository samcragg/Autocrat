namespace Transform.Managed.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Autocrat.Transform.Managed;
    using FluentAssertions;
    using Mono.Cecil;
    using Xunit;

    public class AssemblyLoaderTests : IDisposable
    {
        private readonly AssemblyLoader loader = new AssemblyLoader();
        private readonly List<string> tempAssemblies = new List<string>();
        private readonly string tempDirectory;
        private readonly string workingDirectory;

        // This test simulates an issue where MSBuild is running the task from
        // a different working directory so Cecil is unable to find the other
        // assemblies that are co-located with the Compiler
        private AssemblyLoaderTests()
        {
            this.workingDirectory = Directory.GetCurrentDirectory();
            this.tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(this.tempDirectory);
            Directory.SetCurrentDirectory(this.tempDirectory);
        }

        public void Dispose()
        {
            static void Try(Action action)
            {
                try
                {
                    action();
                }
                catch
                {
                }
            }

            foreach (string assembly in this.tempAssemblies)
            {
                Try(() => File.Delete(assembly));
            }

            Try(() => Directory.Delete(this.tempDirectory, recursive: true));
            Directory.SetCurrentDirectory(this.workingDirectory);
        }

        public sealed class LoadTests : AssemblyLoaderTests
        {
            [Fact]
            public void ShouldAddAllTheModules()
            {
                string mainModuleName = null;
                string assemblyPath = this.CreateAssembly(this.tempDirectory, assembly =>
                {
                    mainModuleName = assembly.MainModule.Name;
                });

                this.loader.Load(assemblyPath);

                this.loader.Modules.Select(m => m.Name)
                    .Should().Contain(new[] { mainModuleName });
            }

            [Fact]
            public void ShouldResolveAssembliesInTheCompilerDirectory()
            {
                TypeReference typeInTempDirectory = this.CreateAssemblyWithType(this.tempDirectory);
                TypeReference typeInCompilerDirectory = this.CreateAssemblyWithType(
                    Path.GetDirectoryName(typeof(AssemblyLoader).Assembly.Location));

                using AssemblyDefinition assembly = this.loader.Load(
                    typeInTempDirectory.Module.Assembly.Name.Name + ".dll");

                TypeReference reference = assembly.MainModule.ImportReference(typeInCompilerDirectory);

                reference.Invoking(tr => tr.Resolve())
                    .Should().NotThrow();
            }

            private string CreateAssembly(string directory, Action<AssemblyDefinition> configure)
            {
                string uniqueId = Guid.NewGuid().ToString();
                string assemblyName = "TestAssembly" + uniqueId;
                using var assembly = AssemblyDefinition.CreateAssembly(
                    new AssemblyNameDefinition(assemblyName, new Version(1, 0)),
                    "TestModule" + uniqueId,
                    ModuleKind.Dll);

                configure(assembly);

                string assemblyFile = Path.Combine(directory, assemblyName + ".dll");
                assembly.Write(assemblyFile);
                this.tempAssemblies.Add(assemblyFile);
                return assemblyFile;
            }

            private TypeReference CreateAssemblyWithType(string directory)
            {
                TypeDefinition type = null;
                this.CreateAssembly(directory, assembly =>
                {
                    type = new TypeDefinition("", assembly.Name + "_TestClass", default);
                    assembly.MainModule.Types.Add(type);
                });
                return type;
            }
        }
    }
}
