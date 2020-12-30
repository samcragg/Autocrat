// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Mono.Cecil;
    using SR = System.Reflection;

    /// <content>
    /// Contains the nested <see cref="References"/> class.
    /// </content>
    internal partial class JsonDeserializerBuilder
    {
        private class References
        {
            private readonly TypeReference listType;
            private readonly ModuleDefinition module;
            private readonly TypeReference nullableType;

            private readonly Dictionary<string, MethodReference> readerMethods
                = new Dictionary<string, MethodReference>();

            internal References(ModuleDefinition module)
            {
                this.module = module;
                this.listType = module.ImportReference(typeof(List<>));
                this.nullableType = module.ImportReference(typeof(Nullable<>));

                this.FormatException = module.ImportReference(
                    typeof(FormatException).GetConstructor(new[] { typeof(string) }));

                this.ReaderRead = module.ImportReference(
                    typeof(Utf8JsonReader).GetMethod(nameof(Utf8JsonReader.Read)));

                this.ReaderTokenType = module.ImportReference(
                    typeof(Utf8JsonReader).GetProperty(nameof(Utf8JsonReader.TokenType))!.GetGetMethod());

                this.ReadOnlySpanType = module.ImportReference(
                    typeof(ReadOnlySpan<byte>));

                this.ReaderTrySkip = module.ImportReference(
                    typeof(Utf8JsonReader).GetMethod(nameof(Utf8JsonReader.TrySkip)));

                this.ReaderValueSpan = module.ImportReference(
                    typeof(Utf8JsonReader).GetProperty(nameof(Utf8JsonReader.ValueSpan))!.GetGetMethod());

                this.Utf8JsonReaderRefType = module.ImportReference(
                    typeof(Utf8JsonReader).MakeByRefType());
            }

            internal MethodReference FormatException { get; }

            internal MethodReference ReaderRead { get; }

            internal MethodReference ReaderTokenType { get; }

            internal MethodReference ReaderTrySkip { get; }

            internal MethodReference ReaderValueSpan { get; }

            internal TypeReference ReadOnlySpanType { get; }

            internal TypeReference Utf8JsonReaderRefType { get; }

            internal MethodReference GetMethod(string name)
            {
                if (!this.readerMethods.TryGetValue(name, out MethodReference? method))
                {
                    method = this.module.ImportReference(typeof(Utf8JsonReader).GetMethod(name));
                    this.readerMethods.Add(name, method);
                }

                return method;
            }

            internal MethodReference ListConstructor(TypeReference type)
            {
                return this.MakeGenericMethod(
                    MakeGeneric(this.listType, type),
                    typeof(List<>).GetConstructor(Type.EmptyTypes));
            }

            internal MethodReference ListMethod(TypeReference type, string name)
            {
                return this.MakeGenericMethod(
                    MakeGeneric(this.listType, type),
                    typeof(List<>).GetMethod(name));
            }

            internal GenericInstanceType ListType(TypeReference type)
            {
                return MakeGeneric(this.listType, type);
            }

            internal MethodReference NullableConstructor(TypeReference type)
            {
                return this.MakeGenericMethod(
                    MakeGeneric(this.nullableType, type),
                    typeof(Nullable<>).GetConstructors().Single());
            }

            internal GenericInstanceType NullableType(TypeReference type)
            {
                return MakeGeneric(this.nullableType, type);
            }

            private static GenericInstanceType MakeGeneric(TypeReference type, TypeReference genericArg)
            {
                var genericType = new GenericInstanceType(type);
                genericType.GenericArguments.Add(genericArg);
                return genericType;
            }

            private MethodReference MakeGenericMethod(
                GenericInstanceType type,
                SR.MethodBase? method)
            {
                MethodReference methodRef = this.module.ImportReference(method);
                methodRef.DeclaringType = type;
                return methodRef;
            }
        }
    }
}
