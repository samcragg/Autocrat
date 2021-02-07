// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.IO;
    using Autocrat.Transform.Managed;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Provides an MSBuild task to transform a managed assembly.
    /// </summary>
    public sealed class AutocratManagedTask : AutocratTaskBase
    {
        /// <summary>
        /// Gets or sets the filename of the generated managed assembly.
        /// </summary>
        [Required]
        public string OutputAssembly { get; set; } = null!;

        /// <summary>
        /// Gets or sets the filename to save the exported data to.
        /// </summary>
        [Required]
        public string OutputExports { get; set; } = null!;

        /// <summary>
        /// Gets or sets the filename of the source assembly.
        /// </summary>
        [Required]
        public string Source { get; set; } = null!;

        /// <inheritdoc />
        protected override bool Transform()
        {
            using var assembly = new OutputFile(this.OutputAssembly);
            using var pdb = new OutputFile(Path.ChangeExtension(this.OutputAssembly, ".pdb"));
            using var exports = new OutputFile(this.OutputExports);
            var managedCompiler = new ManagedCompiler(
                this.Source,
                assembly,
                pdb,
                exports);

            return managedCompiler.Transform();
        }
    }
}
