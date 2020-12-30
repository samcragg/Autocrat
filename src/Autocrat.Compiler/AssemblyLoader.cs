// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.IO;
    using Mono.Cecil;

    /// <summary>
    /// Allows the loading of managed assemblies.
    /// </summary>
    internal class AssemblyLoader
    {
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

            return AssemblyDefinition.ReadAssembly(path, parameters);
        }

        private static IAssemblyResolver CreateResolver()
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(
                Path.GetDirectoryName(typeof(AssemblyLoader).Assembly.Location));
            return resolver;
        }
    }
}
