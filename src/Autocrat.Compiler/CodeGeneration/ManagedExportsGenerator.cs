// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System;
    using System.Linq;
    using Autocrat.NativeAdapters;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Builds a method to register the generated managed types to the native
    /// code (such as the worker and configuration classes).
    /// </summary>
    internal class ManagedExportsGenerator
    {
        /// <summary>
        /// Represents the name of the generated class.
        /// </summary>
        internal const string GeneratedClassName = "ManagedTypes";

        /// <summary>
        /// Represents the name of the generated method.
        /// </summary>
        internal const string GeneratedMethodName = "RegisterManagedTypes";

        // This class creates a hook for initializing the managed code and for
        // registering types with the native code. It is called at start-up from
        // the main thread before any configuration is loaded. This allows us
        // to register the worker class constructors and the generated
        // configuration classes before any other code is called.

        /// <summary>
        /// Gets or sets the generated configuration class.
        /// </summary>
        public virtual TypeDefinition? ConfigClass { get; set; }

        /// <summary>
        /// Gets or sets the generated worker registration class.
        /// </summary>
        public virtual TypeDefinition? WorkersClass { get; set; }

        /// <summary>
        /// Emits the generated code to the specified module.
        /// </summary>
        /// <param name="module">Where to emit the new code to.</param>
        public virtual void Emit(ModuleDefinition module)
        {
            //// public sealed class ManagedTypes
            //// {
            ////     [UnmanagedCallersOnly(...)]
            ////     public static void RegisterManagedTypes()
            ////     {
            ////         ...
            ////     }
            //// }
            TypeDefinition type = CecilHelper.AddClass(module, GeneratedClassName);
            var method = new MethodDefinition(
                GeneratedMethodName,
                Constants.PublicStaticMethod,
                module.TypeSystem.Void);
            type.Methods.Add(method);
            CecilHelper.AddUmanagedAttribute(method);

            ILProcessor il = method.Body.GetILProcessor();
            this.EmitRegisterConfiguration(module, il);
            this.EmitRegisterWorkers(il);
            il.Emit(OpCodes.Ret);
            CecilHelper.OptimizeBody(method);
        }

        private static MethodDefinition GetMethod(TypeDefinition type, string name)
        {
            return type.Methods
                .Single(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private void EmitRegisterConfiguration(ModuleDefinition module, ILProcessor il)
        {
            if (this.ConfigClass == null)
            {
                return;
            }

            MethodDefinition delegateConstructor = module
                .ImportReference(typeof(ConfigService.LoadConfiguration))
                .Resolve()
                .Methods
                .Single(m => m.IsConstructor);

            MethodReference initializeMethod = module.ImportReference(
                typeof(ConfigService).GetMethod(nameof(ConfigService.Initialize)));

            MethodDefinition readConfigMethod = GetMethod(
                this.ConfigClass,
                ConfigResolver.ReadConfigurationMethod);

            //// new ConfigService.LoadConfiguration(null, &ApplicationConfiguration.ReadConfig)
            il.Emit(OpCodes.Ldnull); // It's a static method, so no target
            il.Emit(OpCodes.Ldftn, readConfigMethod);
            il.Emit(OpCodes.Newobj, module.ImportReference(delegateConstructor));

            //// ConfigService.Initialize(...delegate...)
            il.Emit(OpCodes.Call, initializeMethod);
        }

        private void EmitRegisterWorkers(ILProcessor il)
        {
            if (this.WorkersClass == null)
            {
                return;
            }

            MethodDefinition registerMethod = GetMethod(
                this.WorkersClass,
                WorkerRegisterGenerator.GeneratedMethodName);

            //// Workers.RegisterWorkerTypes()
            il.Emit(OpCodes.Call, registerMethod);
        }
    }
}
