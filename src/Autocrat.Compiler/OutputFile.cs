// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.IO;
    using Autocrat.Common;

    /// <summary>
    /// Represents a file to write output to.
    /// </summary>
    internal sealed class OutputFile : IOutputFile
    {
        private readonly Lazy<Stream> stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputFile"/> class.
        /// </summary>
        /// <param name="path">The full path for the output file.</param>
        public OutputFile(string path)
        {
            this.Path = path;
            this.stream = new Lazy<Stream>(this.OpenWrite);
        }

        /// <inheritdoc />
        public string Path { get; }

        /// <inheritdoc />
        public Stream Stream => this.stream.Value;

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.stream.IsValueCreated)
            {
                this.stream.Value.Dispose();
            }
        }

        private Stream OpenWrite()
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(this.Path));
            return File.Create(this.Path);
        }
    }
}
