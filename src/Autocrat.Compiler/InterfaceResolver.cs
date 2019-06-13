// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Allows the matching of concrete types to an interface.
    /// </summary>
    internal class InterfaceResolver
    {
        private readonly Dictionary<Type, HashSet<Type>> interfaceClasses =
            new Dictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Adds the specified class types as candidates when resolving
        /// interface types.
        /// </summary>
        /// <param name="classes">The collection of classes to add.</param>
        public virtual void AddKnownClasses(IEnumerable<Type> classes)
        {
            foreach (Type classType in classes)
            {
                if (classType.IsClass && !classType.IsAbstract)
                {
                    this.RegisterClass(classType, classType);
                }
            }
        }

        /// <summary>
        /// Finds the classes that implement the specified interface.
        /// </summary>
        /// <param name="interfaceType">The type of the interface.</param>
        /// <returns>A list of all classes that implement the specified type.</returns>
        public virtual IReadOnlyCollection<Type> FindClasses(Type interfaceType)
        {
            if (this.interfaceClasses.TryGetValue(interfaceType, out HashSet<Type> mappedClasses))
            {
                return mappedClasses;
            }
            else
            {
                return Array.Empty<Type>();
            }
        }

        private void RegisterClass(Type serviceType, Type classType)
        {
            this.RegisterMapping(serviceType, classType);
            foreach (Type interfaceType in classType.GetInterfaces())
            {
                this.RegisterMapping(serviceType, interfaceType);
            }

            if (classType.BaseType != typeof(object))
            {
                this.RegisterClass(serviceType, classType.BaseType);
            }
        }

        private void RegisterMapping(Type classType, Type interfaceType)
        {
            if (!this.interfaceClasses.TryGetValue(interfaceType, out HashSet<Type> mappedClasses))
            {
                mappedClasses = new HashSet<Type>();
                this.interfaceClasses.Add(interfaceType, mappedClasses);
            }

            mappedClasses.Add(classType);
        }
    }
}
