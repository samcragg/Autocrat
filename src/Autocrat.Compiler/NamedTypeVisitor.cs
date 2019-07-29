// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Extracts the nested type symbols from a symbol.
    /// </summary>
    internal class NamedTypeVisitor : SymbolVisitor
    {
        private readonly HashSet<INamedTypeSymbol> types = new HashSet<INamedTypeSymbol>();

        /// <summary>
        /// Gets the visited symbols.
        /// </summary>
        public IReadOnlyCollection<INamedTypeSymbol> Types => this.types;

        /// <inheritdoc />
        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            this.types.Add(symbol);
        }

        /// <inheritdoc />
        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (INamespaceOrTypeSymbol childSymbol in symbol.GetMembers())
            {
                childSymbol.Accept(this);
            }
        }
    }
}
