// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.Logging
{
    using System;

    /// <summary>
    /// Provides logging utility functions.
    /// </summary>
    internal interface ILogger
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
        void Error(string message, params object[] messageArgs);

        /// <summary>
        /// Logs an error using the message from the given exception context.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
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
