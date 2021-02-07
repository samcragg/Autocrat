// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;

    /// <summary>
    /// Allows the resolving of constructor parameters for a type.
    /// </summary>
    internal class ConstructorResolver
    {
        private readonly InterfaceResolver interfaceResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorResolver"/> class.
        /// </summary>
        /// <param name="interfaceResolver">
        /// Used to resolve classes implementing interfaces.
        /// </param>
        public ConstructorResolver(InterfaceResolver interfaceResolver)
        {
            this.interfaceResolver = interfaceResolver;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorResolver"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected ConstructorResolver()
        {
            this.interfaceResolver = null!;
        }

        /// <summary>
        /// Gets the constructor method for the specified type.
        /// </summary>
        /// <param name="classType">The type to find the constructor on.</param>
        /// <returns>The constructor to invoke to create the type.</returns>
        public virtual MethodDefinition GetConstructor(TypeReference classType)
        {
            return classType.Resolve().Methods
                .Where(m => m.IsConstructor && m.IsPublic)
                .OrderByDescending(m => m.Parameters.Count)
                .First();
        }

        /// <summary>
        /// Finds the classes for injecting into the constructor.
        /// </summary>
        /// <param name="constructor">The constructor to resolve.</param>
        /// <returns>The types needed by the constructor.</returns>
        /// <remarks>
        /// The types returned will be in the order expected by the constructor.
        /// </remarks>
        public virtual IReadOnlyCollection<TypeReference> GetParameters(MethodDefinition constructor)
        {
            if (!constructor.HasParameters)
            {
                return Array.Empty<TypeReference>();
            }
            else
            {
                // Force evaluation eagerly in case there are issues with any
                // of the dependencies
                return constructor.Parameters
                    .Select(p => this.ResolveParameter(p.ParameterType))
                    .ToList();
            }
        }

        private static TypeReference? GetArrayType(TypeReference type)
        {
            // Note we can't interrogate the interfaces for Array to see if any
            // are compatible:
            //
            // https://docs.microsoft.com/en-gb/dotnet/api/system.array
            //
            // Single-dimensional arrays implement the IList<T>, ICollection<T>,
            // IEnumerable<T>, IReadOnlyList<T> and IReadOnlyCollection<T>
            // generic interfaces. The implementations are provided to arrays at
            // run time, and as a result, the generic interfaces do not appear
            // in the declaration syntax for the Array class.
            static bool IsArrayRuntimeInterface(string name)
            {
                return name switch
                {
                    "ICollection`1" or
                    "IEnumerable`1" or
                    "IList`1" or
                    "IReadOnlyCollection`1" or
                    "IReadOnlyList`1" => true,
                    _ => false,
                };
            }

            if (type.IsArray)
            {
                return type;
            }
            else if (type.IsGenericInstance &&
                     string.Equals(type.Namespace, "System.Collections.Generic", StringComparison.Ordinal) &&
                     IsArrayRuntimeInterface(type.Name))
            {
                return new ArrayType(((GenericInstanceType)type).GenericArguments[0]);
            }
            else
            {
                return null;
            }
        }

        private TypeReference ResolveClass(TypeReference type)
        {
            IReadOnlyCollection<TypeReference> classes =
                 this.interfaceResolver.FindClasses(type);

            return classes.Count switch
            {
                0 => throw new InvalidOperationException(
                       "Unable to find a class for the dependency " + type.FullName),

                1 => classes.First(),

                _ => throw new InvalidOperationException(
                        "Multiple dependencies found for " + type.FullName),
            };
        }

        private TypeReference ResolveParameter(TypeReference type)
        {
            TypeReference? arrayType = GetArrayType(type);
            if (arrayType != null)
            {
                return arrayType;
            }
            else
            {
                return this.ResolveClass(type);
            }
        }
    }
}
