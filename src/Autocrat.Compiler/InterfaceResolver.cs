// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Allows the matching of concrete types to an interface.
    /// </summary>
    internal class InterfaceResolver
    {
        private readonly Dictionary<ITypeSymbol, HashSet<INamedTypeSymbol>> interfaceClasses =
            new Dictionary<ITypeSymbol, HashSet<INamedTypeSymbol>>();

        /// <summary>
        /// Adds the specified class types as candidates when resolving
        /// interface types.
        /// </summary>
        /// <param name="classes">The collection of classes to add.</param>
        public virtual void AddKnownClasses(IEnumerable<ITypeSymbol> classes)
        {
            foreach (ITypeSymbol classType in classes)
            {
                if (!classType.IsAbstract &&
                    classType.IsReferenceType &&
                    (classType is INamedTypeSymbol namedType))
                {
                    this.RegisterClass(namedType, namedType);
                }
            }
        }

        /// <summary>
        /// Finds the classes that implement the specified interface.
        /// </summary>
        /// <param name="interfaceType">The type of the interface.</param>
        /// <returns>A list of all classes that implement the specified type.</returns>
        public virtual IReadOnlyCollection<INamedTypeSymbol> FindClasses(ITypeSymbol interfaceType)
        {
            if (this.interfaceClasses.TryGetValue(interfaceType, out HashSet<INamedTypeSymbol> mappedClasses))
            {
                return mappedClasses;
            }
            else
            {
                return Array.Empty<INamedTypeSymbol>();
            }
        }

        private void RegisterClass(INamedTypeSymbol serviceType, INamedTypeSymbol classType)
        {
            this.RegisterMapping(serviceType, classType);
            foreach (INamedTypeSymbol interfaceType in classType.Interfaces)
            {
                this.RegisterMapping(serviceType, interfaceType);
            }

            if (classType.BaseType != null)
            {
                this.RegisterClass(serviceType, classType.BaseType);
            }
        }

        private void RegisterMapping(INamedTypeSymbol classType, INamedTypeSymbol interfaceType)
        {
            if (!this.interfaceClasses.TryGetValue(interfaceType, out HashSet<INamedTypeSymbol> mappedClasses))
            {
                mappedClasses = new HashSet<INamedTypeSymbol>();
                this.interfaceClasses.Add(interfaceType, mappedClasses);
            }

            mappedClasses.Add(classType);
        }
    }
}
