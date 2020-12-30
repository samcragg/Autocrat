// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <content>
    /// Contains the nested <see cref="ILContext"/> class.
    /// </content>
    internal partial class JsonDeserializerBuilder
    {
        private class ILContext
        {
            internal ILContext(ModuleDefinition module, ILProcessor il)
            {
                this.IL = il;
                this.Module = module;
                this.References = new References(module);

                this.Reader = new ParameterDefinition(
                    "reader",
                    ParameterAttributes.None,
                    this.References.Utf8JsonReaderRefType);
            }

            internal ILProcessor IL { get; }

            internal ModuleDefinition Module { get; }

            internal ParameterDefinition Reader { get; }

            internal References References { get; }

            internal VariableDefinition CreateLocal(TypeReference type)
            {
                var local = new VariableDefinition(type);
                this.IL.Body.Variables.Add(local);
                return local;
            }
        }
    }
}
