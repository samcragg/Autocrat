// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Abstractions;
    using Autocrat.Compiler.Logging;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Extracts the types created by the <see cref="IWorkerFactory"/> interface.
    /// </summary>
    internal class WorkerFactoryVisitor : CSharpSyntaxWalker
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly SemanticModel? model;
        private readonly INamedTypeSymbol workerFactoryInterface;
        private readonly HashSet<INamedTypeSymbol> workerTypes = new HashSet<INamedTypeSymbol>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerFactoryVisitor"/> class.
        /// </summary>
        /// <param name="compilation">The compilation to visit.</param>
        public WorkerFactoryVisitor(Compilation compilation)
        {
            INamedTypeSymbol? workerFactory = compilation.GetTypeByMetadataName(
                "Autocrat.Abstractions." + nameof(IWorkerFactory));
            if (workerFactory == null)
            {
                throw new InvalidOperationException("Autocrat.Abstractions assembly is not loaded.");
            }

            this.workerFactoryInterface = workerFactory;
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                this.model = compilation.GetSemanticModel(tree);
                this.Visit(tree.GetRoot());
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerFactoryVisitor"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected WorkerFactoryVisitor()
        {
            this.workerFactoryInterface = null!;
        }

        /// <summary>
        /// Gets the generic arguments passed into the IWorkerFactory::GetWorker methods.
        /// </summary>
        public virtual IReadOnlyCollection<INamedTypeSymbol> WorkerTypes => this.workerTypes;

        /// <inheritdoc />
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (this.model != null)
            {
                SymbolInfo info = this.model.GetSymbolInfo(node.Expression);
                if (info.Symbol?.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)info.Symbol;
                    if (string.Equals(method.Name, nameof(IWorkerFactory.GetWorkerAsync), StringComparison.Ordinal) &&
                        SymbolEqualityComparer.Default.Equals(method.ContainingType, this.workerFactoryInterface))
                    {
                        var type = (INamedTypeSymbol)method.TypeArguments[0];
                        this.logger.Info("Discovered worker type: {0}", type.ToDisplayString());
                        this.workerTypes.Add(type);
                    }
                }
            }

            base.VisitInvocationExpression(node);
        }
    }
}
