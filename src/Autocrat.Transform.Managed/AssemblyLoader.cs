// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Autocrat.Common;
    using Mono.Cecil;

    /// <summary>
    /// Allows the loading of managed assemblies.
    /// </summary>
    internal class AssemblyLoader
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly List<ModuleDefinition> modules = new List<ModuleDefinition>();
        private readonly HashSet<string> scannedAssemblies = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the discovered modules from the loaded assemblies.
        /// </summary>
        public virtual IReadOnlyList<ModuleDefinition> Modules => this.modules;

        /// <summary>
        /// Loads the specified assembly.
        /// </summary>
        /// <param name="path">The path of the assembly to load.</param>
        /// <returns>The assembly definition.</returns>
        public virtual AssemblyDefinition Load(string path)
        {
            var parameters = new ReaderParameters
            {
                AssemblyResolver = CreateResolver(),
                ReadingMode = ReadingMode.Immediate,
            };

            var assembly = AssemblyDefinition.ReadAssembly(path, parameters);
            this.scannedAssemblies.Add(assembly.FullName);
            this.AddModules(assembly);
            return assembly;
        }

        private static IAssemblyResolver CreateResolver()
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(
                Path.GetDirectoryName(typeof(AssemblyLoader).Assembly.Location));
            return resolver;
        }

        private void AddModules(AssemblyDefinition assembly)
        {
            if (assembly.Name.Name.StartsWith("System.", StringComparison.Ordinal))
            {
                return;
            }

            this.logger.Debug("Scanning {0}", assembly.Name);
            foreach (ModuleDefinition module in assembly.Modules)
            {
                this.modules.Add(module);
                foreach (AssemblyNameReference reference in module.AssemblyReferences)
                {
                    if (this.scannedAssemblies.Add(reference.FullName))
                    {
                        this.AddModules(module.AssemblyResolver.Resolve(reference));
                    }
                }
            }
        }
    }
}
