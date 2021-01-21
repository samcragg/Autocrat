// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Common
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents a file to write output to.
    /// </summary>
    public interface IOutputFile : IDisposable
    {
        /// <summary>
        /// Gets the full path for the file.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets the stream to write to.
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// Ensures the output file exists on disk.
        /// </summary>
        void EnsureExists();
    }
}