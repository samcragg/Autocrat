// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeRewriting
{
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Collections.Generic;

    /// <summary>
    /// Facilitates the visiting of CIL metadata and instructions.
    /// </summary>
    internal class CilVisitor
    {
        /// <summary>
        /// Gets the current module information.
        /// </summary>
        protected ModuleDefinition Module => this.Body.Method.Module;

        /// <summary>
        /// Gets the current method body information.
        /// </summary>
        protected MethodBody Body { get; private set; } = null!;

        /// <summary>
        /// Visits the specified instruction.
        /// </summary>
        /// <param name="body">The method body containing the instruction.</param>
        /// <param name="instruction">Contains the instruction data.</param>
        public virtual void Visit(MethodBody body, Instruction instruction)
        {
            this.Body = body;
            this.VisitInstruction(instruction);
            this.Body = null!;
        }

        /// <summary>
        /// Called when an argument is loaded onto the stack.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="index">The index of the argument to load.</param>
        protected virtual void OnLoadArgument(Instruction instruction, int index)
        {
            ParameterDefinition? parameter = this.GetParameter(index);
            if (parameter != null)
            {
                this.OnLoadValue(instruction, parameter.ParameterType);
            }
        }

        /// <summary>
        /// Called when a field is loaded onto the stack.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="field">The reference to the field to load.</param>
        protected virtual void OnLoadField(Instruction instruction, FieldReference field)
        {
            this.OnLoadValue(instruction, field.FieldType);
        }

        /// <summary>
        /// Called when a local variable is loaded onto the stack.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="index">The index of the variable to load.</param>
        protected virtual void OnLoadLocal(Instruction instruction, int index)
        {
            this.OnLoadValue(instruction, this.Body.Variables[index].VariableType);
        }

        /// <summary>
        /// Called when a value is loaded onto the stack.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="type">The type of the value being loaded.</param>
        protected virtual void OnLoadValue(Instruction instruction, TypeReference type)
        {
        }

        /// <summary>
        /// Called when a method is called.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="method">The reference to the method to call.</param>
        protected virtual void OnMethodCall(Instruction instruction, MethodReference method)
        {
        }

        /// <summary>
        /// Called when a new object is pushed to the stack.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="constructor">The constructor used to initialise the object.</param>
        protected virtual void OnNewObj(Instruction instruction, MethodReference constructor)
        {
        }

        /// <summary>
        /// Called when a value from the stack is stored into an argument.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="index">The index of the argument to save to.</param>
        protected virtual void OnStoreArgument(Instruction instruction, int index)
        {
            ParameterDefinition? parameter = this.GetParameter(index);
            if (parameter != null)
            {
                this.OnStoreValue(instruction, parameter.ParameterType);
            }
        }

        /// <summary>
        /// Called when a value from the stack is stored into a field.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="field">The reference to the field to save to.</param>
        protected virtual void OnStoreField(Instruction instruction, FieldReference field)
        {
            this.OnStoreValue(instruction, field.FieldType);
        }

        /// <summary>
        /// Called when a value from the stack is stored into a local variable.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="index">The index of the variable to save to.</param>
        protected virtual void OnStoreLocal(Instruction instruction, int index)
        {
            this.OnStoreValue(instruction, this.Body.Variables[index].VariableType);
        }

        /// <summary>
        /// Called when a value from the stack is stored.
        /// </summary>
        /// <param name="instruction">The instruction information.</param>
        /// <param name="type">The type of the value being stored.</param>
        protected virtual void OnStoreValue(Instruction instruction, TypeReference type)
        {
        }

        /// <summary>
        /// Replaces the specified instruction with the new value.
        /// </summary>
        /// <param name="existing">The instruction to replace.</param>
        /// <param name="replacement">The replacement instruction.</param>
        protected void Replace(Instruction existing, Instruction replacement)
        {
            Collection<Instruction> instructions = this.Body.Instructions;
            int index = instructions.IndexOf(existing);
            instructions[index] = replacement;
        }

        private ParameterDefinition? GetParameter(int index)
        {
            if (this.Body.Method.HasThis)
            {
                if (index == 0)
                {
                    return null;
                }

                index--;
            }

            return this.Body.Method.Parameters[index];
        }

        private void VisitInstruction(Instruction instruction)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Call:
                case Code.Callvirt:
                    this.OnMethodCall(instruction, (MethodReference)instruction.Operand);
                    break;

                case Code.Ldarg:
                case Code.Ldarg_S:
                case Code.Ldarga:
                case Code.Ldarga_S:
                    this.OnLoadArgument(instruction, ((ParameterDefinition)instruction.Operand).Sequence);
                    break;

                case Code.Ldarg_0:
                case Code.Ldarg_1:
                case Code.Ldarg_2:
                case Code.Ldarg_3:
                    this.OnLoadArgument(instruction, instruction.OpCode.Code - Code.Ldarg_0);
                    break;

                case Code.Ldfld:
                case Code.Ldflda:
                case Code.Ldsfld:
                case Code.Ldsflda:
                    this.OnLoadField(instruction, (FieldReference)instruction.Operand);
                    break;

                case Code.Ldloc:
                case Code.Ldloc_S:
                case Code.Ldloca:
                case Code.Ldloca_S:
                    this.OnLoadLocal(instruction, ((VariableDefinition)instruction.Operand).Index);
                    break;

                case Code.Ldloc_0:
                case Code.Ldloc_1:
                case Code.Ldloc_2:
                case Code.Ldloc_3:
                    this.OnLoadLocal(instruction, instruction.OpCode.Code - Code.Ldloc_0);
                    break;

                case Code.Newobj:
                    this.OnNewObj(instruction, (MethodReference)instruction.Operand);
                    break;

                case Code.Starg:
                case Code.Starg_S:
                    this.OnStoreArgument(instruction, ((ParameterDefinition)instruction.Operand).Sequence);
                    break;

                case Code.Stfld:
                case Code.Stsfld:
                    this.OnStoreField(instruction, (FieldReference)instruction.Operand);
                    break;

                case Code.Stloc:
                case Code.Stloc_S:
                    this.OnStoreLocal(instruction, ((VariableDefinition)instruction.Operand).Index);
                    break;

                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                    this.OnStoreLocal(instruction, instruction.OpCode.Code - Code.Stloc_0);
                    break;
            }
        }
    }
}
