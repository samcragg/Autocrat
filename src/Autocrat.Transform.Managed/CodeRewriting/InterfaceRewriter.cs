// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeRewriting
{
    using System;
    using System.Linq;
    using Autocrat.Common;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Rewrites an interface to use the static methods on a class.
    /// </summary>
    internal class InterfaceRewriter : CilVisitor
    {
        // This class allows us to "implement" and interface at compile time.
        // Given the following interface:
        ////
        //// interface IMyService
        //// {
        ////     void InterfaceMethod(int argument);
        //// }
        ////
        // On usage we'd like to transform this:
        ////
        //// myService.InterfaceMethod(123);
        ////
        // into this:
        ////
        //// ClassMarkedAsImplementingMyService.InterfaceMethod(123);
        ////
        // This allows the class "implementing" the interface to control how
        // the native method that actually uses the handle gets called (for
        // example, to convert .NET types to native primitive types)
        private readonly KnownTypes knownTypes;
        private readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceRewriter"/> class.
        /// </summary>
        /// <param name="knownTypes">Contains the discovered types.</param>
        public InterfaceRewriter(KnownTypes knownTypes)
        {
            this.knownTypes = knownTypes;
        }

        /// <inheritdoc />
        protected override void OnLoadValue(Instruction instruction, TypeReference type)
        {
            if (this.knownTypes.ShouldRewrite(type))
            {
                this.RemoveLoadingOfThisArg(instruction);
                this.Body.Instructions.Remove(instruction);
            }
        }

        /// <inheritdoc />
        protected override void OnMethodCall(Instruction instruction, MethodReference method)
        {
            TypeDefinition? classType = this.knownTypes.FindClassForInterface(method.DeclaringType);
            if (classType != null)
            {
                this.logger.Debug("Writing call to {0}", method.FullName);

                // Prevent using CallVirt on static methods
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = classType.Methods
                    .FirstOrDefault(m => string.Equals(m.Name, method.Name, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException("Unable to rewrite " + method.FullName + " using " + classType.FullName);
            }
        }

        /// <inheritdoc />
        protected override void OnStoreValue(Instruction instruction, TypeReference type)
        {
            if (this.knownTypes.ShouldRewrite(type))
            {
                this.RemoveLoadingOfThisArg(instruction);
                this.Body.Instructions.Remove(instruction);
            }
        }

        private void RemoveLoadingOfThisArg(Instruction instruction)
        {
            // We need to remove the loading of 'this' for instance fields
            if (instruction.Operand is FieldReference field)
            {
                FieldDefinition definition = field.Resolve();
                if (!definition.IsStatic && (instruction.Previous.OpCode.Code == Code.Ldarg_0))
                {
                    this.Body.Instructions.Remove(instruction.Previous);
                }
            }
        }
    }
}
