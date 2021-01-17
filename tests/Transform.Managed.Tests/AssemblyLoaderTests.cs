namespace Transform.Managed.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
            try
            {
                foreach (string assembly in this.tempAssemblies)
                {
                    File.Delete(assembly);
                }

                Directory.SetCurrentDirectory(this.workingDirectory);
                Directory.Delete(this.tempDirectory, recursive: true);
            }
            catch
            {
            }
        }

        public sealed class LoadTests : AssemblyLoaderTests
        {
            private static int AssemblyCount = 0;

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

            private TypeReference CreateAssemblyWithType(string directory)
            {
                AssemblyCount++;
                string assemblyName = "TestAssembly" + AssemblyCount;
                using var assembly = AssemblyDefinition.CreateAssembly(
                    new AssemblyNameDefinition(assemblyName, new Version(1, 0)),
                    "TestModule" + AssemblyCount,
                    ModuleKind.Dll);

                var type = new TypeDefinition("", "TestClass" + AssemblyCount, default);
                assembly.MainModule.Types.Add(type);

                string assemblyFile = Path.Combine(directory, assemblyName + ".dll");
                assembly.Write(assemblyFile);
                this.tempAssemblies.Add(assemblyFile);

                return type;
            }
        }
    }
}
