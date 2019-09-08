// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    /// <summary>
    /// Manages the various streams for the output of information.
    /// </summary>
    [SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "This class is not inherited from.")]
    internal class OutputStreams : IDisposable
    {
        private readonly Lazy<Stream> assembly;
        private readonly Lazy<Stream> source;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputStreams"/> class.
        /// </summary>
        /// <param name="assembly">The filename of the generated managed assembly.</param>
        /// <param name="source">The filename of the generated native source code.</param>
        public OutputStreams(string assembly, string source)
        {
            this.assembly = new Lazy<Stream>(() => OpenWrite(assembly));
            this.source = new Lazy<Stream>(() => OpenWrite(source));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputStreams"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected OutputStreams()
        {
        }

        /// <summary>
        /// Gets the output stream for the managed assembly.
        /// </summary>
        public virtual Stream Assembly => this.assembly.Value;

        /// <summary>
        /// Gets the output stream for the native source code.
        /// </summary>
        public virtual Stream Source => this.source.Value;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.assembly.IsValueCreated)
            {
                this.assembly.Value.Dispose();
            }

            if (this.source.IsValueCreated)
            {
                this.source.Value.Dispose();
            }
        }

        private static Stream OpenWrite(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return File.Open(path, FileMode.Create);
        }
    }
}
