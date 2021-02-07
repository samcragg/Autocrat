// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;
    using Mono.Collections.Generic;
    using SR = System.Reflection;

    /// <summary>
    /// Provides utility methods for working with the Cecil API.
    /// </summary>
    internal static class CecilHelper
    {
        /// <summary>
        /// Adds the specified attribute to the method.
        /// </summary>
        /// <typeparam name="T">The type of the attribute to add.</typeparam>
        /// <param name="method">The method to add the attribute to.</param>
        /// <param name="properties">The attribute properties to set.</param>
        public static void AddAttribute<T>(
            MethodDefinition method,
            params (string Name, object? Value)[] properties)
        {
            ModuleDefinition module = method.Module;
            MethodReference constructor = module.ImportReference(
                typeof(T).GetConstructor(Type.EmptyTypes));

            var attribute = new CustomAttribute(constructor);
            foreach ((string name, object? value) in properties)
            {
                SR.PropertyInfo propertyInfo = typeof(T).GetProperty(name)
                    ?? throw new InvalidOperationException("Unknown property " + name);

                attribute.Properties.Add(new CustomAttributeNamedArgument(
                    name,
                    new CustomAttributeArgument(
                        module.ImportReference(propertyInfo.PropertyType),
                        value)));
            }

            method.CustomAttributes.Add(attribute);
        }

        /// <summary>
        /// Adds a new type with the specified name to the module.
        /// </summary>
        /// <param name="module">Where to add the type to.</param>
        /// <param name="name">The name of the new class.</param>
        /// <returns>The new class definition.</returns>
        public static TypeDefinition AddClass(ModuleDefinition module, string name)
        {
            const TypeAttributes publicSealed =
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.Public |
                TypeAttributes.Sealed;

            var type = new TypeDefinition(
                string.Empty,
                name,
                publicSealed,
                module.TypeSystem.Object);
            module.Types.Add(type);
            return type;
        }

        /// <summary>
        /// Adds the UnmanagedCallersOnlyAttribute to a method.
        /// </summary>
        /// <param name="method">The method to add the attribute to.</param>
        public static void AddUmanagedAttribute(MethodDefinition method)
        {
            AddAttribute<UnmanagedCallersOnlyAttribute>(
                method,
                (nameof(UnmanagedCallersOnlyAttribute.CallingConvention), CallingConvention.Cdecl),
                (nameof(UnmanagedCallersOnlyAttribute.EntryPoint), method.Name));
        }

        /// <summary>
        /// Finds an attribute of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the attribute to find.</typeparam>
        /// <param name="type">The type to search for the attribute on.</param>
        /// <returns>The attribute data if found; otherwise, <c>null</c>.</returns>
        public static CustomAttribute? FindAttribute<T>(TypeDefinition type)
        {
            return type.CustomAttributes.FirstOrDefault(
                a => a.AttributeType.FullName == typeof(T).FullName);
        }

        /// <summary>
        /// Gets the default object constructor.
        /// </summary>
        /// <param name="module">The current module.</param>
        /// <returns>A reference to the constructor.</returns>
        public static MethodReference ObjectConstructor(ModuleDefinition module)
        {
            return module.ImportReference(
                module.TypeSystem.Object.Resolve().GetConstructors().Single());
        }

        /// <summary>
        /// Optimizes the IL instructions in the method body.
        /// </summary>
        /// <param name="method">The method to optimize.</param>
        /// <returns>The passed in method.</returns>
        public static MethodDefinition OptimizeBody(MethodDefinition method)
        {
            OptimizeNops(method.Body.Instructions);
            method.Body.Optimize();
            return method;
        }

        private static void OptimizeNops(Collection<Instruction> instructions)
        {
            // We can't remove the last nop in case something is trying to
            // branch to it (when we remove an instruction we check if anything
            // is referencing it and, if so, point them to the next instruction
            // instead). Note we're also going backwards through the collection
            // so we can remove them without effecting the iteration.
            for (int i = instructions.Count - 2; i >= 0; i--)
            {
                if (instructions[i].OpCode.Code == Code.Nop)
                {
                    UpdateReferences(instructions, instructions[i], instructions[i + 1]);
                    instructions.RemoveAt(i);
                }
            }
        }

        private static void UpdateReferences(
            Collection<Instruction> instructions,
            Instruction oldValue,
            Instruction newValue)
        {
            foreach (Instruction instruction in instructions)
            {
                if (instruction.Operand == oldValue)
                {
                    instruction.Operand = newValue;
                }
            }
        }
    }
}
