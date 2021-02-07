// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Common;
    using Mono.Cecil;

    /// <summary>
    /// Generates the code for reading runtime configuration.
    /// </summary>
    internal class ConfigGenerator
    {
        private readonly Dictionary<string, TypeDefinition?> deserializers =
            new Dictionary<string, TypeDefinition?>(StringComparer.Ordinal);

        private readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Gets or sets the method used to create the deserializer builder.
        /// </summary>
        internal static Func<ConfigGenerator, TypeDefinition, JsonDeserializerBuilder> CreateBuilder { get; set; }
            = (cg, td) => new JsonDeserializerBuilder(cg, td);

        /// <summary>
        /// Gets the deserializer for the specified type.
        /// </summary>
        /// <param name="type">The type for the deserializer to return.</param>
        /// <returns>The type reference for the deserializer class.</returns>
        public virtual TypeDefinition GetClassFor(TypeReference type)
        {
            if (!this.deserializers.TryGetValue(type.FullName, out TypeDefinition? deserializer))
            {
                // Create a marker to avoid cyclic dependencies
                this.logger.Debug("Generating deserializer for {0}", type.FullName);
                this.deserializers.Add(type.FullName, null);
                deserializer = this.GenerateClass(type);
                this.deserializers[type.FullName] = deserializer;
            }

            return deserializer ?? throw new InvalidOperationException(
                "There is a cyclic dependency deserializing '" + type.FullName + "'");
        }

        private TypeDefinition GenerateClass(TypeReference type)
        {
            TypeDefinition definition = type.Resolve();
            JsonDeserializerBuilder builder = CreateBuilder(this, definition);
            foreach (PropertyDefinition property in definition.Properties)
            {
                if (property.SetMethod != null)
                {
                    builder.AddProperty(property);
                }
            }

            return builder.GenerateClass(type.Module);
        }
    }
}
