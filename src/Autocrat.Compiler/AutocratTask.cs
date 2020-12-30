// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Autocrat.Compiler.Logging;
    using Microsoft.Build.Framework;
    using Mono.Cecil;

    /// <summary>
    /// Provides an MSBuild task to transform an assembly.
    /// </summary>
    public sealed class AutocratTask : Microsoft.Build.Utilities.Task
    {
        private readonly Logging.ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Gets or sets the description of the program.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the filename of the generates managed assembly.
        /// </summary>
        [Required]
        public string OutputAssembly { get; set; } = null!;

        /// <summary>
        /// Gets or sets the filename of the generates native source code.
        /// </summary>
        [Required]
        public string OutputSource { get; set; } = null!;

        /// <summary>
        /// Gets or sets the filename of the source assembly.
        /// </summary>
        [Required]
        public string Source { get; set; } = null!;

        /// <summary>
        /// Gets or sets the version of the program.
        /// </summary>
        public string? Version { get; set; }

        /// <inheritdoc />
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the main entry point.")]
        public override bool Execute()
        {
            try
            {
                LogManager.SetLogger(this.Log);

                using var output = new OutputStreams(this.OutputAssembly, this.OutputSource);
                var factory = new ServiceFactory();
                return this.Transform(
                    output,
                    factory,
                    m => new CodeGenerator(factory, m));
            }
            catch (Exception ex)
            {
                this.Log.LogErrorFromException(ex);
                return false;
            }
        }

        /// <summary>
        /// Represents the main entry point of the task with the dependencies
        /// injected in.
        /// </summary>
        /// <param name="output">Where to save the generated output to.</param>
        /// <param name="factory">Used to create services.</param>
        /// <param name="createGenerator">
        /// Used to create a class that generates the code.
        /// </param>
        /// <returns><c>true</c> on success; otherwise, <c>false</c>.</returns>
        internal bool Transform(
            OutputStreams output,
            ServiceFactory factory,
            Func<ModuleDefinition, CodeGenerator> createGenerator)
        {
            this.logger.Info("Loading assembly");
            AssemblyDefinition assembly = factory.CreateAssemblyLoader().Load(this.Source);

            this.logger.Info("Generating code");
            CodeGenerator generator = createGenerator(assembly.MainModule);

            this.logger.Info("Emitting assembly");
            generator.EmitAssembly(output.Assembly, output.AssemblyPdb);

            this.logger.Info("Emitting native source");
            generator.EmitNativeCode(this.Version, this.Description, output.Source);
            return true;
        }
    }
}
