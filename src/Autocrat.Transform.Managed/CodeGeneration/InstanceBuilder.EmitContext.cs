// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System;
    using System.Collections.Immutable;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <content>
    /// Contains the nested <see cref="EmitContext"/> struct.
    /// </content>
    internal partial class InstanceBuilder
    {
        private readonly struct EmitContext
        {
            private readonly ImmutableHashSet<string> resolved;

            public EmitContext(TypeReference type, ILProcessor processor)
            {
                this.Processor = processor;
                this.Type = type;
                this.resolved = ImmutableHashSet<string>.Empty.Add(type.FullName);
            }

            public EmitContext(EmitContext context, TypeReference type)
            {
                this.Processor = context.Processor;

                // We don't do cyclic checks on array types to allow creating
                // an array of that type (i.e. new Class[] { new Class() })
                if (type.IsArray)
                {
                    this.resolved = context.resolved;
                    this.Type = type.GetElementType();
                }
                else
                {
                    this.Type = type;
                    this.resolved = context.resolved.Add(type.FullName);
                    if (this.resolved.Count == context.resolved.Count)
                    {
                        throw new InvalidOperationException(
                            "There is a cyclic dependency resolving '" + type.FullName + "'");
                    }
                }
            }

            public ILProcessor Processor { get; }

            public TypeReference Type { get; }
        }
    }
}
