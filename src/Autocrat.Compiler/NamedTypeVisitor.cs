// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.Collections;
    using System.Collections.Generic;
    using Autocrat.Abstractions;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Extracts the nested type symbols from a symbol.
    /// </summary>
    internal class NamedTypeVisitor : SymbolVisitor
    {
        private readonly Dictionary<ITypeSymbol, ITypeSymbol> interfacesToRewrite = new Dictionary<ITypeSymbol, ITypeSymbol>();
        private readonly ISet<INamedTypeSymbol> types = new HashSet<INamedTypeSymbol>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedTypeVisitor"/> class.
        /// </summary>
        public NamedTypeVisitor()
        {
            this.Types = new KnownTypes(this);
        }

        /// <summary>
        /// Gets the visited symbols.
        /// </summary>
        public IKnownTypes Types { get; }

        /// <inheritdoc />
        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            this.types.Add(symbol);

            if (symbol.TypeKind == TypeKind.Class)
            {
                this.CheckForInterfaceAttribute(symbol);
            }
        }

        /// <inheritdoc />
        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (INamespaceOrTypeSymbol childSymbol in symbol.GetMembers())
            {
                childSymbol.Accept(this);
            }
        }

        private void CheckForInterfaceAttribute(INamedTypeSymbol symbol)
        {
            AttributeData? rewriteInterface =
                RoslynHelper.FindAttribute<RewriteInterfaceAttribute>(symbol);

            object? argument = rewriteInterface?.ConstructorArguments[0].Value;
            if (!(argument is null))
            {
                this.interfacesToRewrite.Add((ITypeSymbol)argument, symbol);
            }
        }

        private class KnownTypes : IKnownTypes
        {
            private readonly NamedTypeVisitor owner;

            public KnownTypes(NamedTypeVisitor owner)
            {
                this.owner = owner;
            }

            public int Count => this.owner.types.Count;

            public ITypeSymbol? FindClassForInterface(ITypeSymbol symbol)
            {
                if (this.owner.interfacesToRewrite.TryGetValue(symbol, out ITypeSymbol? classSymbol))
                {
                    return classSymbol;
                }
                else
                {
                    return null;
                }
            }

            public IEnumerator<INamedTypeSymbol> GetEnumerator()
            {
                return this.owner.types.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
