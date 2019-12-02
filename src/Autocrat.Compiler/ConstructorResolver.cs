// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Operations;

    /// <summary>
    /// Allows the resolving of constructor parameters for a type.
    /// </summary>
    internal class ConstructorResolver
    {
        private readonly Compilation compilation;
        private readonly InterfaceResolver interfaceResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorResolver"/> class.
        /// </summary>
        /// <param name="compilation">Represents the compiler.</param>
        /// <param name="interfaceResolver">
        /// Used to resolve classes implementing interfaces.
        /// </param>
        public ConstructorResolver(Compilation compilation, InterfaceResolver interfaceResolver)
        {
            this.interfaceResolver = interfaceResolver;
            this.compilation = compilation;
        }

        /// <summary>
        /// Finds the classes for injecting into the constructor.
        /// </summary>
        /// <param name="classType">The type to find the constructor for.</param>
        /// <returns>The types needed by the constructor.</returns>
        /// <remarks>
        /// The types returned will be in the order expected by the constructor.
        /// </remarks>
        public virtual IReadOnlyList<ITypeSymbol> GetParameters(INamedTypeSymbol classType)
        {
            ImmutableArray<IParameterSymbol> constructorParameters =
                classType.Constructors
                         .Select(c => c.Parameters)
                         .OrderByDescending(p => p.Length)
                         .FirstOrDefault();

            if ((constructorParameters == null) || (constructorParameters.Length == 0))
            {
                return Array.Empty<ITypeSymbol>();
            }
            else
            {
                var types = new List<ITypeSymbol>(constructorParameters.Length);
                types.AddRange(constructorParameters.Select(this.ResolveParameterType));
                return types;
            }
        }

        private ITypeSymbol? GetArrayDependencyType(ITypeSymbol type)
        {
            static bool ContainsSingleGenericArgument(INamedTypeSymbol classType)
            {
                return classType.IsGenericType && (classType.TypeArguments.Length == 1);
            }

            if (type is IArrayTypeSymbol)
            {
                return type;
            }
            else if ((type is INamedTypeSymbol classType) && ContainsSingleGenericArgument(classType))
            {
                IArrayTypeSymbol arrayType = this.compilation.CreateArrayTypeSymbol(
                    classType.TypeArguments[0]);

                CommonConversion conversion = this.compilation.ClassifyCommonConversion(
                    arrayType,
                    type);

                if (conversion.Exists)
                {
                    return arrayType;
                }
            }

            return null;
        }

        private ITypeSymbol ResolveParameterType(IParameterSymbol parameter)
        {
            ITypeSymbol? arrayType = this.GetArrayDependencyType(parameter.Type);
            if (arrayType != null)
            {
                return arrayType;
            }

            IReadOnlyCollection<ITypeSymbol> classes = this.interfaceResolver.FindClasses(
                parameter.Type);

            return classes.Count switch
            {
                0 => throw new InvalidOperationException(
                       "Unable to find a class for the dependency " + parameter.Type.ToDisplayString()),

                1 => classes.First(),

                _ => throw new InvalidOperationException(
                        "Multiple dependencies found for " + parameter.Type.ToDisplayString()),
            };
        }
    }
}
