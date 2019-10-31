// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using NLog;

    /// <summary>
    /// Contains the main entry point of the program.
    /// </summary>
    internal static class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Generates code that will be executed by the bootstrapper.
        /// </summary>
        /// <param name="assembly">The filename of the generated managed assembly.</param>
        /// <param name="source">The filename of the generated native source code.</param>
        /// <param name="args">The project files to transform.</param>
        /// <returns>The exit code of the application.</returns>
        public static Task<int> Main(string assembly, string source, string[] args)
        {
            using var output = new OutputStreams(assembly, source);
            return CompileCodeAsync(
                args,
                output,
                new ProjectLoader(),
                new CodeGenerator());
        }

        /// <summary>
        /// Represents the main entry point of the program with the dependencies
        /// injected in.
        /// </summary>
        /// <param name="projects">The paths of the project files.</param>
        /// <param name="output">Where to save the generated output to.</param>
        /// <param name="loader">Used to load the projects.</param>
        /// <param name="generator">Used to generate the code.</param>
        /// <returns>The exit code of the application.</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the main entry point.")]
        internal static async Task<int> CompileCodeAsync(
            string[] projects,
            OutputStreams output,
            ProjectLoader loader,
            CodeGenerator generator)
        {
            try
            {
                Logger.Info("Loading projects");
                Compilation[] compilations = await loader
                    .GetCompilationsAsync(projects)
                    .ConfigureAwait(false);

                Logger.Info("Generating code");
                foreach (Compilation compilation in compilations)
                {
                    generator.Add(compilation);
                }

                Logger.Info("Emitting assembly");
                generator.EmitAssembly(output.Assembly);

                Logger.Info("Emitting native source");
                generator.EmitNativeCode(output.Source);

                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error");
                return 1;
            }
        }
    }
}
