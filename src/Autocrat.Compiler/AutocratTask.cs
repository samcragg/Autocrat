// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Autocrat.Common;
    using Autocrat.Transform.Managed;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Provides an MSBuild task to transform an assembly.
    /// </summary>
    public sealed class AutocratTask : Microsoft.Build.Utilities.Task
    {
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
                LogManager.SetLogger(new MSBuildLogger(this.Log));

                using var output = new OutputFile(this.OutputAssembly);
                var managedCompiler = new ManagedCompiler(
                    this.Source,
                    output)
                {
                    Description = this.Description,
                    Version = this.Version,
                };

                return managedCompiler.Transform();
            }
            catch (Exception ex)
            {
                this.Log.LogErrorFromException(ex);
                return false;
            }
        }
    }
}
