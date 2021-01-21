// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Common
{
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Manages instances of logger objects.
    /// </summary>
    public static class LogManager
    {
        private static ILogger logger = new EmptyLogger();

        /// <summary>
        /// Gets the currently active logger.
        /// </summary>
        /// <param name="sourceFile">
        /// The full path of source file that contains the caller.
        /// </param>
        /// <returns>The active logger.</returns>
        public static ILogger GetLogger([CallerFilePath] string sourceFile = "")
        {
            Debug.WriteLine("Logger requested for " + Path.GetFileNameWithoutExtension(sourceFile));
            return logger;
        }

        /// <summary>
        /// Sets the active logger to log to the specified instance.
        /// </summary>
        /// <param name="logger">The instance to log to.</param>
        public static void SetLogger(ILogger logger)
        {
            LogManager.logger = logger;
        }
    }
}
