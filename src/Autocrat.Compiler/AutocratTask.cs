// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Autocrat.Compiler.Logging;
    using Microsoft.Build.Framework;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Provides an MSBuild task to transform an assembly.
    /// </summary>
    public sealed class AutocratTask : Microsoft.Build.Utilities.Task
    {
        private readonly Logging.ILogger logger = LogManager.GetLogger();

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
        /// Gets or sets the assembly references for the project.
        /// </summary>
        [Required]
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Required for MSBuild to be able to inject multiple values.")]
        public string[] References { get; set; } = null!;

        /// <summary>
        /// Gets or sets the source paths.
        /// </summary>
        [Required]
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Required for MSBuild to be able to inject multiple values.")]
        public string[] Sources { get; set; } = null!;

        /// <inheritdoc />
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the main entry point.")]
        public override bool Execute()
        {
            try
            {
                LogManager.SetLogger(this.Log);
                using var output = new OutputStreams(this.OutputAssembly, this.OutputSource);
                Task<bool> task = this.ExecuteAsync(
                    output,
                    new ProjectLoader(),
                    c => new CodeGenerator(new ServiceFactory(c), c));

                return task.GetAwaiter().GetResult();
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
        /// <param name="loader">Used to load the projects.</param>
        /// <param name="createGenerator">
        /// Used to create a class that generates the code.
        /// </param>
        /// <returns><c>true</c> on success; otherwise, <c>false</c>.</returns>
        internal async Task<bool> ExecuteAsync(
            OutputStreams output,
            ProjectLoader loader,
            Func<Compilation, CodeGenerator> createGenerator)
        {
            this.logger.Info("Loading sources");
            Compilation compilation = await loader
                .GetCompilationAsync(this.References, this.Sources)
                .ConfigureAwait(false);

            this.logger.Info("Generating code");
            CodeGenerator generator = createGenerator(compilation);

            this.logger.Info("Emitting assembly");
            generator.EmitAssembly(output.Assembly, output.AssemblyPdb);

            this.logger.Info("Emitting native source");
            generator.EmitNativeCode(output.Source);
            return true;
        }
    }
}
