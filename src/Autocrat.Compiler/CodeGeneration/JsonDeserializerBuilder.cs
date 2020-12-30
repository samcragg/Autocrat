// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Generates classes to deserialize JSON into objects.
    /// </summary>
    internal partial class JsonDeserializerBuilder
    {
        /// <summary>
        /// The suffix added to the class name used to create the name of the
        /// generated deserializer class.
        /// </summary>
        internal const string GeneratedClassSuffix = "_Deserializer";

        /// <summary>
        /// Gets the name of the public method to call in the generated class
        /// to read the JSON data.
        /// </summary>
        internal const string ReadMethodName = "Read";

        private readonly TypeDefinition classType;
        private readonly ConfigGenerator generator;
        private readonly FieldDefinition instanceField;
        private readonly List<PropertyData> properties = new List<PropertyData>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDeserializerBuilder"/> class.
        /// </summary>
        /// <param name="generator">Used to generate custom deserializers.</param>
        /// <param name="classType">The name of the class to deserialize.</param>
        public JsonDeserializerBuilder(ConfigGenerator generator, TypeDefinition classType)
        {
            this.generator = generator;
            this.classType = classType;
            this.instanceField = new FieldDefinition(
                "instance",
                FieldAttributes.Private | FieldAttributes.InitOnly,
                classType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDeserializerBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected JsonDeserializerBuilder()
        {
            this.classType = null!;
            this.generator = null!;
            this.instanceField = null!;
        }

        /// <summary>
        /// Adds a property to be deserialized by the generated class.
        /// </summary>
        /// <param name="property">Contains the property information.</param>
        public virtual void AddProperty(PropertyDefinition property)
        {
            this.properties.Add(new PropertyData(property));
        }

        /// <summary>
        /// Generates the class to deserialize JSON data.
        /// </summary>
        /// <param name="module">The module to emit the class to.</param>
        /// <returns>A new class definition.</returns>
        public virtual TypeDefinition GenerateClass(ModuleDefinition module)
        {
            //// public sealed class Xxx_Deserializer
            TypeDefinition deserializerClass = CecilHelper.AddClass(
                module,
                this.classType.Name + GeneratedClassSuffix);

            deserializerClass.Fields.Add(this.instanceField);
            foreach (MethodDefinition method in this.GetMethods(module))
            {
                method.Body.InitLocals = true;
                CecilHelper.OptimizeBody(method);
                deserializerClass.Methods.Add(method);
            }

            return deserializerClass;
        }

        private static void EmitCompareTokenType(ILContext context, JsonTokenType token)
        {
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, context.References.ReaderTokenType);
            context.IL.Emit(OpCodes.Ldc_I4, (int)token);
        }

        private static void EmitConvertNullable(ILContext context, NullableTypeReference type)
        {
            // We have to convert nullable value types to System.Nullable
            if (type.NullableType != null)
            {
                context.IL.Emit(
                    OpCodes.Newobj,
                    context.References.NullableConstructor(type.Type));
            }
        }

        private static void EmitLdConst(ILProcessor il, object constant)
        {
            switch (constant)
            {
                case long value:
                    il.Emit(OpCodes.Ldc_I8, value);
                    break;

                case ulong value:
                    il.Emit(OpCodes.Ldc_I8, (long)value);
                    break;

                default:
                    il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(constant, NumberFormatInfo.InvariantInfo));
                    break;
            }
        }

        private static void EmitLoadNull(ILContext context, NullableTypeReference type)
        {
            if (type.NullableType != null)
            {
                // For Nullable<T> we must have a local to initialize. Since
                // non-null Nullable values use the constructor, the only
                // locals of type Nullable will be for null values, so if we
                // find an existing one we can use that, otherwise, create it
                // and initialize it to null at the start of the method in case
                // we're currently inside some code that gets branched over.
                VariableDefinition? local = context.IL.Body.Variables.FirstOrDefault(v =>
                {
                    return NullableTypeReference.GetNullableType(
                        v.VariableType,
                        out TypeReference? nullableType) &&
                        string.Equals(nullableType.FullName, type.NullableType.FullName, StringComparison.Ordinal);
                });

                if (local == null)
                {
                    local = context.CreateLocal(type.NullableType);
                    context.IL.Body.Instructions.Insert(
                        0,
                        Instruction.Create(OpCodes.Ldloca, local));
                    context.IL.Body.Instructions.Insert(
                        1,
                        Instruction.Create(OpCodes.Initobj, type.NullableType));
                }

                context.IL.Emit(OpCodes.Ldloc, local);
            }
            else
            {
                context.IL.Emit(OpCodes.Ldnull);
            }
        }

        private static void EmitNew(ILProcessor il, TypeDefinition type)
        {
            MethodDefinition constructor =
                type.Methods
                    .SingleOrDefault(m => m.IsConstructor && (m.Parameters.Count == 0))
                ?? throw new InvalidOperationException("Unable to find default constructor for type " + type.FullName);

            il.Emit(OpCodes.Newobj, constructor);
        }

        private static VariableDefinition EmitReadEnum(ILContext context, TypeReference type)
        {
            TypeDefinition typeDef = type.Resolve();
            TypeReference underlyingType = typeDef.Fields
                .First(f => f.Attributes.HasFlag(FieldAttributes.RTSpecialName))
                .FieldType;

            VariableDefinition switchValue = context.CreateLocal(context.Module.TypeSystem.String);
            VariableDefinition result = context.CreateLocal(type);
            var switchEmitter = new SwitchOnStringEmitter(context.Module, context.IL, switchValue);
            foreach (FieldDefinition field in typeDef.Fields.Where(f => f.HasConstant))
            {
                switchEmitter.Add(
                    field.Name,
                    il =>
                    {
                        EmitLdConst(il, field.Constant);
                        il.Emit(OpCodes.Stloc, result);
                    });
            }

            ////     default: throw FormatException(...)
            switchEmitter.DefaultCase = _ =>
            {
                EmitThrowFormatException(context, "Invalid enumerator value");
            };

            var elseBegin = Instruction.Create(OpCodes.Nop);
            var elseEnd = Instruction.Create(OpCodes.Nop);

            //// if (reader.TokenType == JsonTokenType.Number)
            EmitCompareTokenType(context, JsonTokenType.Number);
            context.IL.Emit(OpCodes.Bne_Un, elseBegin);

            ////     (EnumType)reader.GetIntXx()
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, context.References.GetMethod("Get" + underlyingType.Name));
            context.IL.Emit(OpCodes.Stloc, result);
            context.IL.Emit(OpCodes.Br, elseEnd);

            //// else
            ////     value = reader.GetString()
            ////     switch (value) ...
            context.IL.Append(elseBegin);
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, context.References.GetMethod(nameof(Utf8JsonReader.GetString)));
            context.IL.Emit(OpCodes.Stloc, switchValue);
            switchEmitter.Emit();
            context.IL.Append(elseEnd);

            return result;
        }

        private static void EmitThrowFormatException(ILContext context, string message)
        {
            context.IL.Emit(OpCodes.Ldstr, message);
            context.IL.Emit(OpCodes.Newobj, context.References.FormatException);
            context.IL.Emit(OpCodes.Throw);
        }

        private static string? GetJsonReaderMethod(TypeReference type)
        {
            return type.MetadataType switch
            {
                MetadataType.Boolean => nameof(Utf8JsonReader.GetBoolean),
                MetadataType.Byte => nameof(Utf8JsonReader.GetByte),
                MetadataType.Double => nameof(Utf8JsonReader.GetDouble),
                MetadataType.Int16 => nameof(Utf8JsonReader.GetInt16),
                MetadataType.Int32 => nameof(Utf8JsonReader.GetInt32),
                MetadataType.Int64 => nameof(Utf8JsonReader.GetInt64),
                MetadataType.SByte => nameof(Utf8JsonReader.GetSByte),
                MetadataType.Single => nameof(Utf8JsonReader.GetSingle),
                MetadataType.String => nameof(Utf8JsonReader.GetString),
                MetadataType.UInt16 => nameof(Utf8JsonReader.GetUInt16),
                MetadataType.UInt32 => nameof(Utf8JsonReader.GetUInt32),
                MetadataType.UInt64 => nameof(Utf8JsonReader.GetUInt64),
                _ => GetJsonReadMethodForUnknownMetadataType(type),
            };
        }

        private static string? GetJsonReadMethodForUnknownMetadataType(TypeReference type)
        {
            static bool IsByteArray(TypeReference type)
            {
                if (type is ArrayType arrayType)
                {
                    return arrayType.ElementType.MetadataType == MetadataType.Byte;
                }
                else
                {
                    return false;
                }
            }

            // Must check for the array _before_ the check for System types
            if (IsByteArray(type))
            {
                return nameof(Utf8JsonReader.GetBytesFromBase64);
            }

            if (string.Equals(type.Namespace, nameof(System), StringComparison.Ordinal))
            {
                switch (type.Name)
                {
                    case nameof(DateTime):
                        return nameof(Utf8JsonReader.GetDateTime);

                    case nameof(Decimal):
                        return nameof(Utf8JsonReader.GetDecimal);

                    case nameof(Guid):
                        return nameof(Utf8JsonReader.GetGuid);

                    case nameof(DateTimeOffset):
                        return nameof(Utf8JsonReader.GetDateTimeOffset);
                }
            }

            return null;
        }

        private MethodDefinition CreateConstructor(ModuleDefinition module)
        {
            var constructor = new MethodDefinition(
                Constants.Constructor,
                Constants.PublicConstructor,
                module.TypeSystem.Void);

            ILProcessor il = constructor.Body.GetILProcessor();

            //// object()
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, CecilHelper.ObjectConstructor(module));

            //// this.instance = new T()
            il.Emit(OpCodes.Ldarg_0);
            EmitNew(il, this.classType);
            il.Emit(OpCodes.Stfld, this.instanceField);

            il.Emit(OpCodes.Ret);
            return CecilHelper.OptimizeBody(constructor);
        }

        private MethodDefinition CreateReadMethod(ModuleDefinition module, MethodDefinition readProperty)
        {
            //// public Type Read(ref Utf8JsonReader reader)
            var method = new MethodDefinition(ReadMethodName, Constants.PublicMethod, this.classType);
            var context = new ILContext(module, method.Body.GetILProcessor());
            method.Parameters.Add(context.Reader);

            var beginLoop = Instruction.Create(OpCodes.Nop);
            var endLoop = Instruction.Create(OpCodes.Nop);
            var throwMissingStart = Instruction.Create(OpCodes.Nop);
            var loopBody = Instruction.Create(OpCodes.Nop);
            var methodEnd = Instruction.Create(OpCodes.Nop);

            // We allow to be positioned on the start object for the scenario
            // we're being used by a nested serializer
            //// if (reader.TokenType != JsonTokenType.StartObject)
            ////     if (!reader.Read() || (reader.TokenType != JsonTokenType.StartObject))
            ////         throw new FormatException("Missing start object token")
            EmitCompareTokenType(context, JsonTokenType.StartObject);
            context.IL.Emit(OpCodes.Beq, beginLoop);
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, context.References.ReaderRead);
            context.IL.Emit(OpCodes.Brfalse, throwMissingStart);
            EmitCompareTokenType(context, JsonTokenType.StartObject);
            context.IL.Emit(OpCodes.Beq, beginLoop);
            context.IL.Append(throwMissingStart);
            EmitThrowFormatException(context, "Missing start object token");

            //// while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            ////     this.ReadProperty(ref reader)
            context.IL.Append(loopBody);
            context.IL.Emit(OpCodes.Ldarg_0);
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, readProperty);
            context.IL.Append(beginLoop);
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, context.References.ReaderRead);
            context.IL.Emit(OpCodes.Brfalse, endLoop);
            EmitCompareTokenType(context, JsonTokenType.PropertyName);
            context.IL.Emit(OpCodes.Beq, loopBody);

            //// if (reader.TokenType != JsonTokenType.EndObject)
            ////     throw new FormatException("Missing end object token")
            context.IL.Append(endLoop);
            EmitCompareTokenType(context, JsonTokenType.EndObject);
            context.IL.Emit(OpCodes.Beq, methodEnd);
            EmitThrowFormatException(context, "Missing end object token");

            ////     return this.instance
            context.IL.Append(methodEnd);
            context.IL.Emit(OpCodes.Ldarg_0);
            context.IL.Emit(OpCodes.Ldfld, this.instanceField);
            context.IL.Emit(OpCodes.Ret);

            return method;
        }

        private MethodDefinition CreateReadPropertyMethod(ModuleDefinition module)
        {
            //// private void ReadProperties(ref Utf8JsonReader reader)
            var method = new MethodDefinition("ReadProperties", Constants.PrivateMethod, module.TypeSystem.Void);
            var context = new ILContext(module, method.Body.GetILProcessor());
            method.Parameters.Add(context.Reader);

            //// ReadOnlySpan<byte> value = reader.ValueSpan
            VariableDefinition value = context.CreateLocal(context.References.ReadOnlySpanType);
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, context.References.ReaderValueSpan);
            context.IL.Emit(OpCodes.Stloc, value);

            //// switch (value)
            var switchEmitter = new SwitchOnStringEmitter(context.Module, context.IL, value);
            foreach (PropertyData property in this.properties)
            {
                switchEmitter.Add(property.Name, _ =>
                {
                    //// this.Read_Abc(ref reader)
                    context.IL.Emit(OpCodes.Ldarg_0);
                    context.IL.Emit(OpCodes.Ldarg, context.Reader);
                    context.IL.Emit(OpCodes.Call, property.ReadMethod);
                });
            }

            //// default: // Skip unknown properties
            ////     reader.Read()
            ////     reader.TrySkip()
            switchEmitter.DefaultCase = _ =>
            {
                context.IL.Emit(OpCodes.Ldarg, context.Reader);
                context.IL.Emit(OpCodes.Call, context.References.ReaderRead);
                context.IL.Emit(OpCodes.Pop);
                context.IL.Emit(OpCodes.Ldarg, context.Reader);
                context.IL.Emit(OpCodes.Call, context.References.ReaderTrySkip);
                context.IL.Emit(OpCodes.Pop);
            };
            switchEmitter.Emit();

            context.IL.Emit(OpCodes.Ret);
            return method;
        }

        private VariableDefinition EmitReadArray(ILContext context, NullableTypeReference type)
        {
            if (type.Element == null)
            {
                throw new InvalidOperationException("Unable to determine the array element type");
            }

            var endThrow = Instruction.Create(OpCodes.Nop);
            var loopBody = Instruction.Create(OpCodes.Nop);
            var loopEnd = Instruction.Create(OpCodes.Nop);
            var loopStart = Instruction.Create(OpCodes.Nop);

            TypeReference elementType = type.Element.NullableType ?? type.Element.Type;
            VariableDefinition arrayValue = context.CreateLocal(new ArrayType(elementType));
            VariableDefinition buffer = context.CreateLocal(context.References.ListType(elementType));

            //// if (reader.TokenType != JsonTokenType.StartArray)
            ////     throw new FormatException("Missing start array token")
            EmitCompareTokenType(context, JsonTokenType.StartArray);
            context.IL.Emit(OpCodes.Beq, endThrow);
            EmitThrowFormatException(context, "Missing start array token");
            context.IL.Append(endThrow);

            //// var buffer = new List<T>();
            context.IL.Emit(OpCodes.Newobj, context.References.ListConstructor(elementType));
            context.IL.Emit(OpCodes.Stloc, buffer);

            //// while (...)
            context.IL.Emit(OpCodes.Br, loopStart);

            ////     buffer.Add(...)
            context.IL.Append(loopBody);
            this.EmitReadValueWithNullCheck(
                context,
                type.Element,
                loadValue =>
                {
                    context.IL.Emit(OpCodes.Ldloc, buffer);
                    loadValue();
                    context.IL.Emit(
                        OpCodes.Callvirt,
                        context.References.ListMethod(elementType, nameof(List<object>.Add)));
                });

            //// while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            context.IL.Append(loopStart);
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, context.References.ReaderRead);
            context.IL.Emit(OpCodes.Brfalse, loopEnd);

            EmitCompareTokenType(context, JsonTokenType.EndArray);
            context.IL.Emit(OpCodes.Bne_Un, loopBody);

            //// value = buffer.ToArray()
            context.IL.Append(loopEnd);
            context.IL.Emit(OpCodes.Ldloc, buffer);
            context.IL.Emit(
                OpCodes.Callvirt,
                context.References.ListMethod(elementType, nameof(List<object>.ToArray)));
            context.IL.Emit(OpCodes.Stloc, arrayValue);
            return arrayValue;
        }

        private void EmitReadValue(ILContext context, TypeReference type)
        {
            string? readerMethod = GetJsonReaderMethod(type);
            if (readerMethod != null)
            {
                //// target = reader.GetXxx()
                context.IL.Emit(OpCodes.Ldarg, context.Reader);
                context.IL.Emit(OpCodes.Call, context.References.GetMethod(readerMethod));
            }
            else
            {
                TypeDefinition deserializer = this.generator.GetClassFor(type);
                MethodDefinition readMethod = deserializer.Methods.First(
                    m => string.Equals(m.Name, ReadMethodName, StringComparison.Ordinal));

                //// new TypeDeserializer().Read(ref reader)
                EmitNew(context.IL, deserializer);
                context.IL.Emit(OpCodes.Ldarg, context.Reader);
                context.IL.Emit(OpCodes.Call, readMethod);
            }
        }

        private void EmitReadValueWithNullCheck(
            ILContext context,
            NullableTypeReference type,
            Action<Action> store)
        {
            static bool IsByteArray(TypeReference tr)
            {
                return string.Equals(tr.FullName, "System.Byte[]", StringComparison.Ordinal);
            }

            var end = Instruction.Create(OpCodes.Nop);
            if (type.AllowsNulls)
            {
                var elseBegin = Instruction.Create(OpCodes.Nop);

                //// if (reader.TokenType == JsonTokenType.Null)
                EmitCompareTokenType(context, JsonTokenType.Null);
                context.IL.Emit(OpCodes.Bne_Un, elseBegin);

                ////     value = null
                store(() => EmitLoadNull(context, type));

                //// else
                context.IL.Emit(OpCodes.Br, end);
                context.IL.Append(elseBegin);
            }

            //// value = ...
            if (type.Type.IsArray)
            {
                if (IsByteArray(type.Type))
                {
                    store(() => this.EmitReadValue(context, type.Type));
                }
                else
                {
                    VariableDefinition value = this.EmitReadArray(context, type);
                    store(() => context.IL.Emit(OpCodes.Ldloc, value));
                }
            }
            else
            {
                // TypeDefinition looses the array information, hence the check
                // for array is on the TypeReference
                TypeDefinition definition = type.Type.Resolve();
                if (definition.IsEnum)
                {
                    VariableDefinition value = EmitReadEnum(context, definition);
                    store(() =>
                    {
                        context.IL.Emit(OpCodes.Ldloc, value);
                        EmitConvertNullable(context, type);
                    });
                }
                else
                {
                    store(() =>
                    {
                        this.EmitReadValue(context, definition);
                        EmitConvertNullable(context, type);
                    });
                }
            }

            context.IL.Append(end);
        }

        private MethodDefinition GenerateReadValueMethod(
            ModuleDefinition module,
            PropertyData property)
        {
            //// void Read_Xxx(ref Utf8Reader reader)
            MethodDefinition method = property.ReadMethod;
            var context = new ILContext(module, method.Body.GetILProcessor());
            method.Parameters.Add(context.Reader);

            //// reader.Read()
            context.IL.Emit(OpCodes.Ldarg, context.Reader);
            context.IL.Emit(OpCodes.Call, context.References.ReaderRead);
            context.IL.Emit(OpCodes.Pop);

            //// this.instance.Value = ...
            this.EmitReadValueWithNullCheck(
                context,
                property.Type,
                loadValue =>
                {
                    context.IL.Emit(OpCodes.Ldarg_0);
                    context.IL.Emit(OpCodes.Ldfld, this.instanceField);
                    loadValue();
                    context.IL.Emit(OpCodes.Callvirt, property.SetMethod);
                });

            context.IL.Emit(OpCodes.Ret);
            return method;
        }

        private IEnumerable<MethodDefinition> GetMethods(ModuleDefinition module)
        {
            yield return this.CreateConstructor(module);
            MethodDefinition readProperty = this.CreateReadPropertyMethod(module);
            yield return this.CreateReadMethod(module, readProperty);
            yield return readProperty;

            foreach (PropertyData property in this.properties)
            {
                yield return this.GenerateReadValueMethod(module, property);
            }
        }
    }
}
