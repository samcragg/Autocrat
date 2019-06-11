// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;

    /// <summary>
    /// Contains the main entry point of the program.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        internal static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                Console.WriteLine("Received: '{0}'", arg);
            }
        }
    }
}
