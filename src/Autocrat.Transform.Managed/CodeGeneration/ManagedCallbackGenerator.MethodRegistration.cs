// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System.Collections.Generic;
    using Mono.Cecil;

    /// <content>
    /// Contains the nested <see cref="MethodRegistration"/> class.
    /// </content>
    internal partial class ManagedCallbackGenerator
    {
        private class MethodRegistration
        {
            public MethodRegistration(MethodDefinition method)
            {
                this.Name = method.DeclaringType.Name + "_" + method.Name;
                this.OriginalMethod = method;
            }

            public TypeReference DeclaringType => this.OriginalMethod.DeclaringType;

            public string Name { get; }

            public MethodReference OriginalMethod { get; }

            public IList<ParameterDefinition> Parameters => this.OriginalMethod.Parameters;

            public TypeReference ReturnType => this.OriginalMethod.ReturnType;
        }
    }
}
