// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System.Collections;
    using System.Collections.Generic;
    using Autocrat.Abstractions;
    using Autocrat.Compiler.Logging;
    using Mono.Cecil;

    /// <summary>
    /// Represents a collection of the discovered types.
    /// </summary>
    internal class KnownTypes : IReadOnlyCollection<TypeDefinition>
    {
        private readonly Dictionary<TypeReference, TypeDefinition> interfacesToRewrite =
            new Dictionary<TypeReference, TypeDefinition>(new TypeReferenceEqualityComparer());

        private readonly ILogger logger = LogManager.GetLogger();
        private readonly List<TypeDefinition> types = new List<TypeDefinition>();

        /// <inheritdoc />
        public virtual int Count => this.types.Count;

        /// <summary>
        /// Finds the class marked as rewriting the specified interface, if any.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <returns>
        /// The type definition for the class, or <c>null</c> if one doesn't
        /// exist.
        /// </returns>
        public virtual TypeDefinition? FindClassForInterface(TypeReference interfaceType)
        {
            if (this.interfacesToRewrite.TryGetValue(interfaceType, out TypeDefinition? classType))
            {
                return classType;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        public virtual IEnumerator<TypeDefinition> GetEnumerator()
        {
            return this.types.GetEnumerator();
        }

        /// <summary>
        /// Scans the specified module for types and their interfaces.
        /// </summary>
        /// <param name="module">The module definition to scan.</param>
        public virtual void Scan(ModuleDefinition module)
        {
            foreach (TypeDefinition type in module.Types)
            {
                // Skip the <Module> class, which doesn't inherit from anything
                // (classes inherit from Object and value types from ValueType)
                if (type.BaseType == null)
                {
                    continue;
                }

                this.types.Add(type);
                if (type.IsClass)
                {
                    this.CheckForInterfaceAttribute(type);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the specified type is marked as
        /// being rewritten.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <returns>
        /// <c>true</c> if the specified type should be rewritten to be another
        /// class; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool ShouldRewrite(TypeReference interfaceType)
        {
            return this.interfacesToRewrite.ContainsKey(interfaceType);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void CheckForInterfaceAttribute(TypeDefinition type)
        {
            CustomAttribute? rewriteInterface =
                CecilHelper.FindAttribute<RewriteInterfaceAttribute>(type);

            object? argument = rewriteInterface?.ConstructorArguments[0].Value;
            if (argument is TypeReference tr)
            {
                this.logger.Debug("Recording {0} as implementing {1}", type.Name, tr.Name);
                this.interfacesToRewrite.Add(tr, type);
            }
        }
    }
}
