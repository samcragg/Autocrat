// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using NLog;

    /// <summary>
    /// Contains the main entry point of the program.
    /// </summary>
    internal static class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        internal static void Main(string[] args)
        {
            Logger.Debug("Program invoked with {arguments}.", args);
        }
    }
}
