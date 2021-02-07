// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Provides logging utility functions.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a low importance message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">
        /// Optional arguments for formatting the message string.
        /// </param>
        void Debug(string message, params object[] messageArgs);

        /// <summary>
        /// Logs an error using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">
        /// Optional arguments for formatting the message string.
        /// </param>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Error is not a C# keyword")]
        void Error(string message, params object[] messageArgs);

        /// <summary>
        /// Logs an error using the message from the given exception context.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Error is not a C# keyword")]
        void Error(Exception exception);

        /// <summary>
        /// Logs a normal importance message using the specified string.
        /// </summary>
        /// <param name="message">The message string.</param>
        /// <param name="messageArgs">
        /// Optional arguments for formatting the message string.
        /// </param>
        void Info(string message, params object[] messageArgs);
    }
}
