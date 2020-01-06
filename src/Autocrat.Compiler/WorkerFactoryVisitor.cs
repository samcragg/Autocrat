// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Abstractions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Extracts the types created by the <see cref="IWorkerFactory"/> interface.
    /// </summary>
    internal class WorkerFactoryVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel? model;
        private readonly INamedTypeSymbol workerFactoryInterface;
        private readonly HashSet<ITypeSymbol> workerTypes = new HashSet<ITypeSymbol>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerFactoryVisitor"/> class.
        /// </summary>
        /// <param name="compilation">The compilation to visit.</param>
        public WorkerFactoryVisitor(Compilation compilation)
        {
            this.workerFactoryInterface = compilation.GetTypeByMetadataName(
                "Autocrat.Abstractions." + nameof(IWorkerFactory));

            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                this.model = compilation.GetSemanticModel(tree);
                this.Visit(tree.GetRoot());
            }
        }

        /// <summary>
        /// Gets the generic arguments passed into <see cref="IWorkerFactory.GetWorker{T}"/>.
        /// </summary>
        public IReadOnlyCollection<ITypeSymbol> WorkerTypes => this.workerTypes;

        /// <inheritdoc />
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (this.model != null)
            {
                SymbolInfo info = this.model.GetSymbolInfo(node.Expression);
                if (info.Symbol?.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)info.Symbol;
                    if (string.Equals(method.Name, nameof(IWorkerFactory.GetWorker), StringComparison.Ordinal) &&
                        SymbolEqualityComparer.Default.Equals(method.ContainingType, this.workerFactoryInterface))
                    {
                        this.workerTypes.Add(method.TypeArguments[0]);
                    }
                }
            }

            base.VisitInvocationExpression(node);
        }
    }
}
