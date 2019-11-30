// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Provides utility methods for working with the Roslyn API.
    /// </summary>
    internal static class RoslynHelper
    {
        /// <summary>
        /// Determines whether the specified symbol equals the specified type.
        /// </summary>
        /// <typeparam name="T">The type to compare to.</typeparam>
        /// <param name="symbol">The type symbol.</param>
        /// <returns>
        /// <c>true</c> if the type and symbol have the same name and namespace;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsOfType<T>(INamedTypeSymbol symbol)
        {
            return string.Equals(symbol.Name, typeof(T).Name, StringComparison.Ordinal) &&
                   string.Equals(symbol.ContainingNamespace.ToDisplayString(), typeof(T).Namespace, StringComparison.Ordinal);
        }
    }
}
