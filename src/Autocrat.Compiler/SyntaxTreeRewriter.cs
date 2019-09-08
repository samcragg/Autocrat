// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Abstractions;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Rewrites the syntax trees of the existing code.
    /// </summary>
    internal class SyntaxTreeRewriter
    {
        private const string NativeAdapterAttributeName =
            "Autocrat.Abstractions." + nameof(NativeAdapterAttribute);

        private readonly List<(ITypeSymbol, ITypeSymbol)> adapters = new List<(ITypeSymbol, ITypeSymbol)>();
        private readonly Compilation compilation;
        private readonly Func<SemanticModel, NativeRegisterRewriter> createRewriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxTreeRewriter"/> class.
        /// </summary>
        /// <param name="compilation">Contains the compiled information.</param>
        /// <param name="createRewriter">Creates a NativeRegisterRewriter.</param>
        /// <param name="knownTypes">Contains the discovered types.</param>
        public SyntaxTreeRewriter(
            Compilation compilation,
            Func<SemanticModel, NativeRegisterRewriter> createRewriter,
            IKnownTypes knownTypes)
        {
            this.compilation = compilation;
            this.createRewriter = createRewriter;

            foreach (INamedTypeSymbol type in knownTypes)
            {
                foreach (AttributeData attribute in type.GetAttributes())
                {
                    if (attribute.AttributeClass.ToDisplayString() == NativeAdapterAttributeName)
                    {
                        INamedTypeSymbol interfaceType = type.Interfaces.Single();
                        this.adapters.Add((type, interfaceType));
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxTreeRewriter"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected SyntaxTreeRewriter()
        {
        }

        /// <summary>
        /// Create the syntax tree for the specified source.
        /// </summary>
        /// <param name="tree">The source document.</param>
        /// <returns>The transformed tree.</returns>
        public virtual SyntaxTree Generate(SyntaxTree tree)
        {
            if (!this.compilation.ContainsSyntaxTree(tree))
            {
                return tree;
            }

            SemanticModel model = this.compilation.GetSemanticModel(tree);
            NativeRegisterRewriter rewriter = this.createRewriter(model);
            foreach ((ITypeSymbol adapter, ITypeSymbol interfaceType) in this.adapters)
            {
                rewriter.AddReplacement(interfaceType, adapter);
            }

            SyntaxNode root = rewriter.Visit(tree.GetRoot());
            return tree.WithRootAndOptions(root, tree.Options);
        }
    }
}
