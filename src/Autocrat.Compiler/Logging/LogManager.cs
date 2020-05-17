// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.Logging
{
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Manages instances of logger objects.
    /// </summary>
    internal static class LogManager
    {
        private static ILogger logger = new EmptyLogger();

        /// <summary>
        /// Gets the currently active logger.
        /// </summary>
        /// <returns>The active logger.</returns>
        public static ILogger GetLogger()
        {
            return logger;
        }

        /// <summary>
        /// Sets the active logger to log to the specified instance.
        /// </summary>
        /// <param name="loggingHelper">The instance to log to.</param>
        public static void SetLogger(TaskLoggingHelper loggingHelper)
        {
            logger = new MSBuildLogger(loggingHelper);
        }
    }
}
