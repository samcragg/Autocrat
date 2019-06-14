// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

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
        /// Finds the classes for injecting into the constructor.
        /// </summary>
        /// <param name="classType">The type to find the constructor for.</param>
        /// <returns>The types needed by the constructor.</returns>
        /// <remarks>
        /// The types returned will be in the order expected by the constructor.
        /// </remarks>
        public Type[] GetParameters(Type classType)
        {
            // Classes always have one constructor, hence the call to First
            ParameterInfo[] constructorParameters =
                classType.GetConstructors()
                         .Select(c => c.GetParameters())
                         .OrderByDescending(p => p.Length)
                         .First();

            Array.Sort(constructorParameters, (a, b) => a.Position.CompareTo(b.Position));
            return Array.ConvertAll(constructorParameters, this.ResolveParameterType);
        }

        private static Type GetArrayDependencyType(Type type)
        {
            if (type.IsArray)
            {
                return type;
            }

            if (type.IsGenericType)
            {
                Type[] arguments = type.GetGenericArguments();
                if (arguments.Length == 1)
                {
                    Type arrayType = arguments[0].MakeArrayType();
                    if (type.IsAssignableFrom(arrayType))
                    {
                        return arrayType;
                    }
                }
            }

            return null;
        }

        private Type ResolveParameterType(ParameterInfo parameter)
        {
            Type arrayType = GetArrayDependencyType(parameter.ParameterType);
            if (arrayType != null)
            {
                return arrayType;
            }

            IReadOnlyCollection<Type> classes = this.interfaceResolver.FindClasses(
                parameter.ParameterType);

            switch (classes.Count)
            {
                case 0:
                    throw new InvalidOperationException(
                        "Unable to find a class for the dependency " + parameter.ParameterType.Name);

                case 1:
                    return classes.First();

                default:
                    throw new InvalidOperationException(
                        "Multiple dependencies found for " + parameter.ParameterType.Name);
            }
        }
    }
}
