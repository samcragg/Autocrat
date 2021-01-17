// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeRewriting
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Abstractions;
    using Autocrat.Transform.Managed.Logging;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Extracts the types created by the <see cref="IWorkerFactory"/> interface.
    /// </summary>
    internal class WorkerFactoryVisitor : CilVisitor
    {
        private readonly ILogger logger = LogManager.GetLogger();

        private readonly HashSet<TypeReference> workerTypes =
            new HashSet<TypeReference>(new TypeReferenceEqualityComparer());

        /// <summary>
        /// Gets the generic arguments passed into the IWorkerFactory::GetWorker methods.
        /// </summary>
        public virtual IReadOnlyCollection<TypeReference> WorkerTypes => this.workerTypes;

        /// <inheritdoc />
        protected override void OnMethodCall(Instruction instruction, MethodReference method)
        {
            if (string.Equals(method.Name, nameof(IWorkerFactory.GetWorkerAsync), StringComparison.Ordinal) &&
                string.Equals(method.DeclaringType.FullName, "Autocrat.Abstractions.IWorkerFactory", StringComparison.Ordinal))
            {
                TypeReference type = ((GenericInstanceMethod)method).GenericArguments[0];
                if (this.workerTypes.Add(type))
                {
                    this.logger.Info("Discovered worker type: {0}", type.FullName);
                }
            }
        }
    }
}
