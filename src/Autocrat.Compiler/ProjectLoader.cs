// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Buildalyzer;
    using Buildalyzer.Workspaces;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Loads the C# projects from disk.
    /// </summary>
    internal class ProjectLoader
    {
        private readonly AnalyzerManager analyzerManager = new AnalyzerManager();

        /// <summary>
        /// Gets the compilations for the specified project paths.
        /// </summary>
        /// <param name="paths">The paths of the project files.</param>
        /// <returns>
        /// A task that represents the compilations of the projects.
        /// </returns>
        public virtual Task<Compilation[]> GetCompilationsAsync(string[] paths)
        {
            var tasks = new List<Task<Compilation>>();
            foreach (string path in paths)
            {
                IEnumerable<Project> projects = this.analyzerManager
                    .GetProject(path)
                    .GetWorkspace()
                    .CurrentSolution
                    .Projects;

                tasks.AddRange(projects.Select(p => p.GetCompilationAsync()));
            }

            return Task.WhenAll(tasks.ToArray());
        }
    }
}
