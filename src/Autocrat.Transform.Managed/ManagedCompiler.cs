// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using System.IO;
    using Autocrat.Common;
    using Mono.Cecil;

    /// <summary>
    /// Provides an MSBuild task to transform an assembly.
    /// </summary>
    public sealed class ManagedCompiler
    {
        private readonly string assemblyPath;
        private readonly ServiceFactory factory;
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly IOutputFile output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedCompiler"/> class.
        /// </summary>
        /// <param name="assemblyPath">The managed assembly to load.</param>
        /// <param name="output">Specifies where to write the output to.</param>
        public ManagedCompiler(string assemblyPath, IOutputFile output)
        {
            this.assemblyPath = assemblyPath;
            this.output = output;
            this.factory = new ServiceFactory();
        }

        /// <summary>
        /// Gets or sets the description of the program.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the version of the program.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Transforms the managed assembly, generating a separate managed
        /// assembly with the modifications.
        /// </summary>
        /// <returns><c>true</c> on success; otherwise, <c>false</c>.</returns>
        public bool Transform()
        {
            this.logger.Info("Loading assembly");
            AssemblyDefinition assembly = this.factory
                .CreateAssemblyLoader()
                .Load(this.assemblyPath);

            this.logger.Info("Generating code");
            CodeGenerator generator = this.factory.CreateCodeGenerator(assembly.MainModule);

            this.logger.Info("Emitting assembly");
            this.output.EnsureExists(); // Also ensures the directory is created
            using Stream pdbStream = File.Create(
                Path.ChangeExtension(this.output.Path, ".pdb"));
            generator.EmitAssembly(this.output.Stream, pdbStream);

            //// This needs moving...
            //// this.logger.Info("Emitting native source");
            //// generator.EmitNativeCode(this.Version, this.Description, output.Source);
            return true;
        }
    }
}
