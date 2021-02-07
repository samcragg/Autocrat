// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.IO;
    using Autocrat.Transform.Native;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Provides an MSBuild task to generate additional native source code.
    /// </summary>
    public sealed class AutocratNativeTask : AutocratTaskBase
    {
        /// <summary>
        /// Gets or sets the description of the program.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the filename of the exported information from the
        /// managed task.
        /// </summary>
        [Required]
        public string Exports { get; set; } = null!;

        /// <summary>
        /// Gets or sets the filename of the generated native source code.
        /// </summary>
        [Required]
        public string OutputSource { get; set; } = null!;

        /// <summary>
        /// Gets or sets the version of the program.
        /// </summary>
        public string? Version { get; set; }

        /// <inheritdoc />
        protected override bool Transform()
        {
            var generator = new NativeCodeGenerator
            {
                Description = this.Description,
                Version = this.Version,
            };

            using Stream exports = File.OpenRead(this.Exports);
            using var output = new OutputFile(this.OutputSource);
            generator.Generate(exports, output);
            return true;
        }
    }
}
