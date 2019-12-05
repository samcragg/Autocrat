// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Represents a collection of the discovered types.
    /// </summary>
    internal interface IKnownTypes : IReadOnlyCollection<INamedTypeSymbol>
    {
        /// <summary>
        /// Finds the class marked as rewriting the specified interface, if any.
        /// </summary>
        /// <param name="symbol">The interface type.</param>
        /// <returns>The class symbol, or <c>null</c> if none exist.</returns>
        ITypeSymbol? FindClassForInterface(ITypeSymbol symbol);
    }
}
