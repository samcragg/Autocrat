// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.Logging
{
    using System;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Implements the logger interface without performing any actions.
    /// </summary>
    internal sealed class EmptyLogger : ILogger
    {
        /// <inheritdoc />
        public void Debug(string message, params object[] messageArgs)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc />
        public void Error(Location? location, string message, params object[] messageArgs)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc />
        public void Error(Exception exception)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc />
        public void Info(string message, params object[] messageArgs)
        {
            // Method intentionally left empty.
        }
    }
}
