// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Rewrites the syntax trees of the existing code.
    /// </summary>
    internal class SyntaxTreeRewriter
    {
        private readonly Compilation compilation;
        private readonly Func<SemanticModel, InterfaceRewriter> createRewriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxTreeRewriter"/> class.
        /// </summary>
        /// <param name="compilation">Contains the compiled information.</param>
        /// <param name="createRewriter">Creates a NativeRegisterRewriter.</param>
        public SyntaxTreeRewriter(
            Compilation compilation,
            Func<SemanticModel, InterfaceRewriter> createRewriter)
        {
            this.compilation = compilation;
            this.createRewriter = createRewriter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxTreeRewriter"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected SyntaxTreeRewriter()
        {
            this.compilation = null!;
            this.createRewriter = null!;
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
            InterfaceRewriter rewriter = this.createRewriter(model);
            SyntaxNode root = rewriter.Visit(tree.GetRoot());
            return tree.WithRootAndOptions(root, tree.Options);
        }
    }
}
