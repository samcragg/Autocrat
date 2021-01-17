// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Mono.Cecil;

    /// <summary>
    /// Contains information about a type and its support for null values.
    /// </summary>
    internal sealed class NullableTypeReference
    {
        private NullableTypeReference(byte[] nullableFlags, int depth, TypeReference type)
        {
            this.Type = type;
            if (type.IsValueType)
            {
                if (GetNullableType(type, out TypeReference? elementType))
                {
                    this.AllowsNulls = true;
                    this.NullableType = type;
                    this.Type = elementType;
                }
            }
            else if (depth < nullableFlags.Length)
            {
                this.AllowsNulls = nullableFlags[depth] == 2;
            }
            else if (nullableFlags.Length == 1)
            {
                // Special case for when all the values in the array would be
                // the same
                this.AllowsNulls = nullableFlags[0] == 2;
            }

            if (type is ArrayType array)
            {
                this.Element = new NullableTypeReference(nullableFlags, depth + 1, array.ElementType);
            }
        }

        /// <summary>
        /// Gets a value indicating whether null values are supported or not.
        /// </summary>
        public bool AllowsNulls { get; }

        /// <summary>
        /// Gets the type of the array elements, if any.
        /// </summary>
        public NullableTypeReference? Element { get; }

        /// <summary>
        /// Gets the Nullable&lt;T&gt; type for value types.
        /// </summary>
        public TypeReference? NullableType { get; }

        /// <summary>
        /// Gets the type information.
        /// </summary>
        public TypeReference Type { get; }

        /// <summary>
        /// Creates a new instance from the property information.
        /// </summary>
        /// <param name="property">The property definition.</param>
        /// <returns>
        /// A new instance of the <see cref="NullableTypeReference"/> class.
        /// </returns>
        public static NullableTypeReference Create(PropertyDefinition property)
        {
            return new NullableTypeReference(
                GetNullableFlags(property),
                0,
                property.PropertyType);
        }

        /// <summary>
        /// Gets the nullable element type.
        /// </summary>
        /// <param name="type">The type to extract the information from.</param>
        /// <param name="nullableType">
        /// If this method returns <c>true</c>, will contain the nullable
        /// element type.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified type is a nullable type; otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool GetNullableType(
            TypeReference type,
            [NotNullWhen(true)] out TypeReference? nullableType)
        {
            if (string.Equals(type.Namespace, "System", StringComparison.Ordinal) &&
                string.Equals(type.Name, "Nullable`1", StringComparison.Ordinal))
            {
                nullableType = ((GenericInstanceType)type).GenericArguments[0];
                return true;
            }
            else
            {
                nullableType = null;
                return false;
            }
        }

        [SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null", Justification = "We're using null to enable the use of the null-coalescing operator")]
        private static byte[]? GetNullableAttributeFlags(
            ICustomAttributeProvider provider,
            string attributeName)
        {
            CustomAttribute? attribute = provider.CustomAttributes
                .FirstOrDefault(a => a.AttributeType.FullName == ("System.Runtime.CompilerServices." + attributeName));
            if (attribute is null)
            {
                return null;
            }

            CustomAttributeArgument argument = attribute.ConstructorArguments.Single();
            if (argument.Value is byte b)
            {
                return new byte[] { b };
            }
            else
            {
                return Array.ConvertAll(
                    (CustomAttributeArgument[])argument.Value,
                    a => (byte)a.Value);
            }
        }

        private static byte[] GetNullableFlags(PropertyDefinition property)
        {
            // The compiler will emit an attribute for the whole type that
            // contains the most common value for the NullableAttribute that
            // is applied to properties, so if the property isn't annotated
            // then try looking at the type for the default value
            // https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md
            return GetNullableAttributeFlags(property, "NullableAttribute") ??
                GetNullableAttributeFlags(property.DeclaringType, "NullableContextAttribute") ??
                Array.Empty<byte>();
        }
    }
}
