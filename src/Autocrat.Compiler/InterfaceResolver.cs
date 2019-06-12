// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Allows the matching of concrete types to an interface.
    /// </summary>
    internal class InterfaceResolver
    {
        private readonly Dictionary<Type, List<Type>> interfaceClasses =
            new Dictionary<Type, List<Type>>();

        /// <summary>
        /// Adds the specified class types as candidates when resolving
        /// interface types.
        /// </summary>
        /// <param name="classes">The collection of classes to add.</param>
        public virtual void AddKnownClasses(IEnumerable<Type> classes)
        {
            foreach (Type classType in classes.Where(c => c.IsClass && !c.IsAbstract))
            {
                foreach (Type interfaceType in classType.GetInterfaces())
                {
                    if (!this.interfaceClasses.TryGetValue(interfaceType, out List<Type> mappedClasses))
                    {
                        mappedClasses = new List<Type>();
                        this.interfaceClasses.Add(interfaceType, mappedClasses);
                    }

                    mappedClasses.Add(classType);
                }
            }
        }

        /// <summary>
        /// Finds the classes that implement the specified interface.
        /// </summary>
        /// <param name="interfaceType">The type of the interface.</param>
        /// <returns>A list of all classes that implement the specified type.</returns>
        public virtual IReadOnlyList<Type> FindClasses(Type interfaceType)
        {
            if (this.interfaceClasses.TryGetValue(interfaceType, out List<Type> mappedClasses))
            {
                return mappedClasses;
            }
            else
            {
                return Array.Empty<Type>();
            }
        }
    }
}
