// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using Mono.Cecil;

    /// <summary>
    /// Allows the matching of concrete types to an interface.
    /// </summary>
    internal class InterfaceResolver
    {
        private readonly Dictionary<TypeReference, List<TypeDefinition>> interfaceClasses =
            new Dictionary<TypeReference, List<TypeDefinition>>(new TypeReferenceEqualityComparer());

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceResolver"/> class.
        /// </summary>
        /// <param name="knownTypes">Contains the discovered types.</param>
        public InterfaceResolver(KnownTypes knownTypes)
        {
            foreach (TypeDefinition classType in knownTypes)
            {
                if (classType.IsClass && !classType.IsAbstract)
                {
                    this.RegisterClass(classType, classType);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceResolver"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected InterfaceResolver()
        {
        }

        /// <summary>
        /// Finds the classes that implement the specified interface.
        /// </summary>
        /// <param name="interfaceType">The type of the interface.</param>
        /// <returns>A list of all classes that implement the specified type.</returns>
        public virtual IReadOnlyCollection<TypeDefinition> FindClasses(TypeReference interfaceType)
        {
            if (this.interfaceClasses.TryGetValue(interfaceType, out List<TypeDefinition>? mappedClasses))
            {
                return mappedClasses;
            }
            else
            {
                return Array.Empty<TypeDefinition>();
            }
        }

        private void RegisterClass(TypeDefinition serviceType, TypeDefinition classType)
        {
            this.RegisterMapping(serviceType, classType);
            foreach (InterfaceImplementation interfaceImp in classType.Interfaces)
            {
                this.RegisterMapping(serviceType, interfaceImp.InterfaceType);
            }

            if (classType.BaseType != null)
            {
                this.RegisterClass(serviceType, classType.BaseType.Resolve());
            }
        }

        private void RegisterMapping(TypeDefinition classType, TypeReference interfaceType)
        {
            if (!this.interfaceClasses.TryGetValue(interfaceType, out List<TypeDefinition>? mappedClasses))
            {
                mappedClasses = new List<TypeDefinition>();
                this.interfaceClasses.Add(interfaceType, mappedClasses);
            }

            mappedClasses.Add(classType);
        }
    }
}
