// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System.Collections.Generic;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Creates methods that are exported to native code.
    /// </summary>
    internal partial class ManagedCallbackGenerator
    {
        /// <summary>
        /// Represents the name of the generated class.
        /// </summary>
        internal const string GeneratedClassName = "NativeCallableMethods";

        // This class generates static methods that invoke an instance method
        // so that they can be called from native code, i.e.
        //
        //// class Example
        //// {
        ////     public Example(object injected)
        ////     {
        ////         ...
        ////     }
        ////
        ////     public object Method()
        ////     {
        ////         ...
        ////     }
        //// }
        //
        // will be transformed to:
        //
        //// public static class NativeCallableMethods
        //// {
        ////     [UnmanagedCallersOnly("Example_Method")]
        ////     public static object Example_Method()
        ////     {
        ////         var instance = new Example(new object());
        ////         return instance.Method();
        ////     }
        //// }
        private readonly InstanceBuilder instanceBuilder;
        private readonly List<MethodRegistration> methods = new List<MethodRegistration>();
        private readonly NativeImportGenerator nativeGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedCallbackGenerator"/> class.
        /// </summary>
        /// <param name="instanceBuilder">Used to generate code to create objects.</param>
        /// <param name="nativeGenerator">Used to register the managed methods.</param>
        public ManagedCallbackGenerator(
            InstanceBuilder instanceBuilder,
            NativeImportGenerator nativeGenerator)
        {
            this.instanceBuilder = instanceBuilder;
            this.nativeGenerator = nativeGenerator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedCallbackGenerator"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected ManagedCallbackGenerator()
        {
            this.instanceBuilder = null!;
            this.nativeGenerator = null!;
        }

        /// <summary>
        /// Generates a native callable method for the specified type.
        /// </summary>
        /// <param name="nativeSignature">The format of the native signature.</param>
        /// <param name="method">The method in the type to generate.</param>
        /// <returns>The native index of the generated method.</returns>
        public virtual int AddMethod(string nativeSignature, MethodDefinition method)
        {
            var registration = new MethodRegistration(method);
            this.methods.Add(registration);
            return this.nativeGenerator.RegisterMethod(nativeSignature, registration.Name);
        }

        /// <summary>
        /// Emits the native callable methods to the specified module.
        /// </summary>
        /// <param name="module">Where to add the generated type to.</param>
        public virtual void EmitType(ModuleDefinition module)
        {
            //// public sealed class NativeCallableMethods
            TypeDefinition generatedType = CecilHelper.AddClass(module, GeneratedClassName);
            foreach (MethodRegistration method in this.methods)
            {
                //// [UnmanagedCallersOnly(...)]
                //// public static SomeType Class_Method(...)
                var definition = new MethodDefinition(
                    method.Name,
                    Constants.PublicStaticMethod,
                    method.ReturnType);

                generatedType.Methods.Add(definition);
                CecilHelper.AddUmanagedAttribute(definition);

                foreach (ParameterDefinition parameter in method.Parameters)
                {
                    definition.Parameters.Add(CloneParameter(parameter));
                }

                this.EmitBody(method, definition.Body.GetILProcessor());
                CecilHelper.OptimizeBody(definition);
            }
        }

        private static ParameterDefinition CloneParameter(ParameterDefinition parameter)
        {
            return new ParameterDefinition(
                parameter.Name,
                parameter.Attributes,
                parameter.ParameterType);
        }

        private void EmitBody(MethodRegistration registration, ILProcessor il)
        {
            //// return (new ClassType(...)).Method(arg1, arg2, ... )
            this.instanceBuilder.EmitNewObj(registration.DeclaringType, il);
            for (int i = 0; i < registration.Parameters.Count; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }

            // No need for CallVirt here, as we put a new object of the exact
            // type on the stack
            il.Emit(OpCodes.Call, registration.OriginalMethod);
            il.Emit(OpCodes.Ret);
        }
    }
}
