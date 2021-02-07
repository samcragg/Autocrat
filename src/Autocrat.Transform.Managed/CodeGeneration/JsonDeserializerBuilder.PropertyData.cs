// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using Mono.Cecil;

    /// <content>
    /// Contains the nested <see cref="PropertyData"/> class.
    /// </content>
    internal partial class JsonDeserializerBuilder
    {
        private class PropertyData
        {
            public PropertyData(PropertyDefinition property)
            {
                this.Name = property.Name;
                this.ReadMethod = new MethodDefinition(
                    "Read_" + property.Name,
                    Constants.PrivateMethod,
                    property.Module.TypeSystem.Void);

                this.SetMethod = property.SetMethod;
                this.Type = NullableTypeReference.Create(property);
            }

            public string Name { get; }

            public MethodDefinition ReadMethod { get; }

            public MethodDefinition SetMethod { get; }

            public NullableTypeReference Type { get; }
        }
    }
}
