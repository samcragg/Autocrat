// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using Mono.Cecil;

    /// <content>
    /// Contains the nested <see cref="GeneratedClass"/> class.
    /// </content>
    internal partial class ConfigResolver
    {
        private sealed class GeneratedClass
        {
            public GeneratedClass(TypeDefinition definition, TypeReference configurationClass)
            {
                this.ConfigurationClass = configurationClass;
                this.Definition = definition;

                this.InstanceField = new FieldDefinition(
                    "instance",
                    FieldAttributes.Private | FieldAttributes.Static,
                    definition);
                definition.Fields.Add(this.InstanceField);

                this.RootField = new FieldDefinition(
                    "root",
                    Constants.PrivateReadonly,
                    configurationClass);
                definition.Fields.Add(this.RootField);
            }

            public TypeReference ConfigurationClass { get; }

            public TypeDefinition Definition { get; }

            public FieldDefinition InstanceField { get; }

            public ModuleDefinition Module => this.Definition.Module;

            public FieldDefinition RootField { get; }
        }
    }
}
