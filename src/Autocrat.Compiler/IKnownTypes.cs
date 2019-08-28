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
    }
}
