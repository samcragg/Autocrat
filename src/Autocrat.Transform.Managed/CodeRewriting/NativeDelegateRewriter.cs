// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeRewriting
{
    using Autocrat.Abstractions;
    using Autocrat.Transform.Managed.CodeGeneration;
    using Autocrat.Transform.Managed.Logging;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Rewrites usages of delegates marked as being exported to the native code.
    /// </summary>
    internal class NativeDelegateRewriter : CilVisitor
    {
        // This class changes callback delegates into an index that can be used
        // to register a managed method that is called from the native code.
        // For example, given this:
        //
        //// NativeRegister(this.HandleMessage);
        //
        // Then the following needs to be generated (here 123 is the result
        // when registering the method as an exported one):
        //
        //// NativeRegister(123);
        //
        // The actual generation of that method is done by ManagedCallbackGenerator
        private readonly ManagedCallbackGenerator callbackGenerator;
        private readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeDelegateRewriter"/> class.
        /// </summary>
        /// <param name="callbackGenerator">Generates the managed stub methods.</param>
        public NativeDelegateRewriter(ManagedCallbackGenerator callbackGenerator)
        {
            this.callbackGenerator = callbackGenerator;
        }

        /// <inheritdoc />
        protected override void OnNewObj(Instruction instruction, MethodDefinition constructor)
        {
            // For creating a delegate, we're expecting to see this sequence:
            //// ldarg  <- The "instance" value, will be null for statics
            //// ldftn  <- The method to call
            //// newobj <- The delegate marked with the NativeDelegateAttribute
            string? signature = GetNativeSignature(constructor.DeclaringType);
            if (signature is null)
            {
                return;
            }

            this.logger.Debug(
                "Rewriting native delegate creation of {0}",
                constructor.DeclaringType.Name);

            // Remove the existing instructions
            var method = (MethodDefinition)instruction.Previous.Operand;
            int end = this.Body.Instructions.IndexOf(instruction);
            int start = end - 2;
            for (int i = end; i >= start; i--)
            {
                this.Body.Instructions.RemoveAt(i);
            }

            // Add the new one, which is to load the method handle instead
            int handle = this.callbackGenerator.AddMethod(signature, method);
            this.Body.Instructions.Insert(start, Instruction.Create(OpCodes.Ldc_I4, handle));
        }

        private static string? GetNativeSignature(TypeDefinition declaringType)
        {
            CustomAttribute? nativeDelegate =
                CecilHelper.FindAttribute<NativeDelegateAttribute>(declaringType);

            return nativeDelegate?.ConstructorArguments[0].Value as string;
        }
    }
}
