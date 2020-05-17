﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.Logging
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Provides an adapter for logging to MSBuild.
    /// </summary>
    internal class MSBuildLogger : ILogger
    {
        private readonly TaskLoggingHelper logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSBuildLogger"/> class.
        /// </summary>
        /// <param name="logger">The instance to log to.</param>
        public MSBuildLogger(TaskLoggingHelper logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public void Debug(string message, params object[] messageArgs)
        {
            this.logger.LogMessage(MessageImportance.Low, message, messageArgs);
        }

        /// <inheritdoc />
        public void Error(Location location, string message, params object[] messageArgs)
        {
            FileLinePositionSpan position = location.GetMappedLineSpan();
            this.logger.LogError(
                null,
                null,
                null,
                position.Path,
                position.StartLinePosition.Line,
                position.StartLinePosition.Character,
                position.EndLinePosition.Line,
                position.EndLinePosition.Character,
                message,
                messageArgs);
        }

        /// <inheritdoc />
        public void Error(Exception exception)
        {
            this.logger.LogErrorFromException(exception);
        }

        /// <inheritdoc />
        public void Info(string message, params object[] messageArgs)
        {
            this.logger.LogMessage(MessageImportance.Normal, message, messageArgs);
        }
    }
}