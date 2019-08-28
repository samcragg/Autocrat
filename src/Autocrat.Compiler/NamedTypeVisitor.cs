// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Extracts the nested type symbols from a symbol.
    /// </summary>
    internal class NamedTypeVisitor : SymbolVisitor
    {
        private readonly ISet<INamedTypeSymbol> types = new HashSet<INamedTypeSymbol>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedTypeVisitor"/> class.
        /// </summary>
        public NamedTypeVisitor()
        {
            this.Types = new KnownTypes(this.types);
        }

        /// <summary>
        /// Gets the visited symbols.
        /// </summary>
        public IKnownTypes Types { get; }

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

        private class KnownTypes : IKnownTypes
        {
            private readonly ICollection<INamedTypeSymbol> collection;

            public KnownTypes(ICollection<INamedTypeSymbol> collection)
            {
                this.collection = collection;
            }

            public int Count => this.collection.Count;

            public IEnumerator<INamedTypeSymbol> GetEnumerator()
            {
                return this.collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
