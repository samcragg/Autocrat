// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Native
{
    using System;
    using System.IO;
    using System.Text;
    using Autocrat.Common;

    /// <summary>
    /// Generates native code to adapt the managed code to the runtime library.
    /// </summary>
    public class NativeCodeGenerator
    {
        private readonly MainGenerator main;
        private readonly NativeImportGenerator nativeImports;
        private readonly StaticInitializerGenerator staticInitializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeCodeGenerator"/> class.
        /// </summary>
        public NativeCodeGenerator()
            : this(
                  new MainGenerator(),
                  new NativeImportGenerator(),
                  new StaticInitializerGenerator())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeCodeGenerator"/> class.
        /// </summary>
        /// <param name="main">Used to generate the main entry point.</param>
        /// <param name="nativeImports">Used to generate the native imports.</param>
        /// <param name="staticInitializer">Used to generate static initialization.</param>
        internal NativeCodeGenerator(
            MainGenerator main,
            NativeImportGenerator nativeImports,
            StaticInitializerGenerator staticInitializer)
        {
            this.main = main;
            this.nativeImports = nativeImports;
            this.staticInitializer = staticInitializer;
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
        /// Temporary method to avoid compile errors.
        /// </summary>
        /// <param name="exports">Contains the managed export information.</param>
        /// <param name="map">Contains the generated map file.</param>
        /// <param name="output">Where to write the generated output to.</param>
        public void Generate(Stream exports, Stream map, IOutputFile output)
        {
            if (exports is null)
            {
                throw new ArgumentNullException(nameof(exports));
            }

            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            using var writer = new StreamWriter(output.Stream, Encoding.UTF8);
            this.nativeImports.LoadExports(exports);
            this.nativeImports.WriteTo(writer);

            this.staticInitializer.Load(map);
            this.staticInitializer.WriteTo(writer);

            this.main.Description = this.Description;
            this.main.Version = this.Version;
            this.main.WriteTo(writer);
        }
    }
}
