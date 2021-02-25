// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using Autocrat.Common;
    using Autocrat.NativeAdapters;
    using Mono.Cecil;

    /// <summary>
    /// Transforms a managed assembly to add additional functionality required
    /// by the native compiler.
    /// </summary>
    public sealed class ManagedCompiler
    {
        private readonly IOutputFile assemblyOutput;
        private readonly string assemblyPath;
        private readonly IOutputFile exportsOutput;
        private readonly ServiceFactory factory;
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly IOutputFile pdbOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedCompiler"/> class.
        /// </summary>
        /// <param name="assemblyPath">The managed assembly to load.</param>
        /// <param name="assembly">Where to write the generated assembly.</param>
        /// <param name="pdb">Where to write the debug information.</param>
        /// <param name="exports">Where to write the exported information.</param>
        public ManagedCompiler(
            string assemblyPath,
            IOutputFile assembly,
            IOutputFile pdb,
            IOutputFile exports)
            : this(assemblyPath, assembly, pdb, exports, new ServiceFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedCompiler"/> class.
        /// </summary>
        /// <param name="assemblyPath">The managed assembly to load.</param>
        /// <param name="assembly">Where to write the generated assembly.</param>
        /// <param name="pdb">Where to write the debug information.</param>
        /// <param name="exports">Where to write the exported information.</param>
        /// <param name="factory">Factory to create the services.</param>
        internal ManagedCompiler(
            string assemblyPath,
            IOutputFile assembly,
            IOutputFile pdb,
            IOutputFile exports,
            ServiceFactory factory)
        {
            this.assemblyOutput = assembly;
            this.assemblyPath = assemblyPath;
            this.exportsOutput = exports;
            this.factory = factory;
            this.pdbOutput = pdb;
        }

        /// <summary>
        /// Transforms the managed assembly, generating a separate managed
        /// assembly with the modifications.
        /// </summary>
        /// <returns><c>true</c> on success; otherwise, <c>false</c>.</returns>
        public bool Transform()
        {
            this.logger.Info("Loading assembly");
            AssemblyLoader loader = this.factory.CreateAssemblyLoader();
            KnownTypes knownTypes = this.factory.GetKnownTypes();

            AssemblyDefinition assembly = loader.Load(this.assemblyPath);

            // The NativeAdapters implement some of the interfaces in
            // Abstractions, so make sure its modules are scanned too.
            loader.Load(typeof(ConfigService).Assembly.Location);
            foreach (ModuleDefinition module in loader.Modules)
            {
                knownTypes.Scan(module);
            }

            this.logger.Info("Generating code");
            CodeGenerator generator = this.factory.CreateCodeGenerator(assembly.MainModule);

            this.logger.Info("Emitting assembly");
            generator.EmitAssembly(
                this.assemblyOutput.Stream,
                this.pdbOutput.Stream);

            this.logger.Info("Saving export information");
            this.factory.GetExportedMethods().SerializeTo(
                this.exportsOutput.Stream);

            return true;
        }
    }
}
