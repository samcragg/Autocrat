// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System.Collections.Generic;
    using Autocrat.NativeAdapters;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Builds a method to register the known worker types.
    /// </summary>
    internal class WorkerRegisterGenerator
    {
        /// <summary>
        /// Represents the name of the generated class.
        /// </summary>
        internal const string GeneratedClassName = "Workers";

        /// <summary>
        /// Represents the name of the generated method.
        /// </summary>
        internal const string GeneratedMethodName = "RegisterWorkerTypes";

        // This class goes through every call to IWorkerFactory.GetWorker<Type>()
        // and generates a method that can create the type and registers that
        // method with the native code. This is to allow for dependency
        // injection, for example, this call:
        //
        //// workerFactory.GetWorker<MyClass>()
        //
        // would cause the following to be generated (note that the original
        // call still happens and this is generated in a separate class):
        //
        //// public static void RegisterWorkerTypes()
        //// {
        ////     WorkerFactory.RegisterConstructor<MyClass>(123);
        //// }
        ////
        //// [UnmanagedCallersOnly("CreateMyClass")]
        //// public static object CreateMyClass()
        //// {
        ////     var dependency = new InjectedDependency();
        ////     return new MyClass(dependency);
        //// }
        //
        // where 123 is the method handle for the CreateMyClass method.
        private readonly ExportedMethods exportedMethods;
        private readonly IReadOnlyCollection<TypeReference> factoryTypes;
        private readonly InstanceBuilder instanceBuilder;
        private MethodDefinition? workerFactoryRegister;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerRegisterGenerator"/> class.
        /// </summary>
        /// <param name="instanceBuilder">
        /// Used to generate code to create new objects.
        /// </param>
        /// <param name="factoryTypes">The worker types to register.</param>
        /// <param name="exportedMethods">Used to register the managed methods.</param>
        public WorkerRegisterGenerator(
            InstanceBuilder instanceBuilder,
            IReadOnlyCollection<TypeReference> factoryTypes,
            ExportedMethods exportedMethods)
        {
            this.factoryTypes = factoryTypes;
            this.instanceBuilder = instanceBuilder;
            this.exportedMethods = exportedMethods;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerRegisterGenerator"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected WorkerRegisterGenerator()
        {
            this.factoryTypes = null!;
            this.instanceBuilder = null!;
            this.exportedMethods = null!;
        }

        /// <summary>
        /// Emits the generated code to the specified module.
        /// </summary>
        /// <param name="module">Where to emit the new code to.</param>
        /// <returns>The generated type.</returns>
        public virtual TypeDefinition EmitWorkerClass(ModuleDefinition module)
        {
            TypeDefinition workers = CecilHelper.AddClass(module, GeneratedClassName);

            //// public static void RegisterWorkerTypes()
            var registerWorker = new MethodDefinition(
                GeneratedMethodName,
                Constants.PublicStaticMethod,
                module.TypeSystem.Void);
            workers.Methods.Add(registerWorker);

            ILProcessor il = registerWorker.Body.GetILProcessor();
            foreach (TypeReference type in this.factoryTypes)
            {
                int handle = this.EmitCreateMethod(workers, type);
                this.EmitCallRegisterConstructor(il, type, handle);
            }

            il.Emit(OpCodes.Ret);
            CecilHelper.OptimizeBody(registerWorker);
            return workers;
        }

        private void EmitCallRegisterConstructor(ILProcessor il, TypeReference type, int handle)
        {
            ModuleDefinition module = il.Body.Method.Module;
            if (this.workerFactoryRegister is null)
            {
                this.workerFactoryRegister = module
                    .ImportReference(typeof(WorkerFactory).GetMethod(nameof(WorkerFactory.RegisterConstructor)))
                    .Resolve();
            }

            var registerConstructor = new GenericInstanceMethod(this.workerFactoryRegister);
            registerConstructor.GenericArguments.Add(type);

            //// WorkerFactory.RegisterConstructor<Type>(123)
            il.Emit(OpCodes.Ldc_I4, handle);
            il.Emit(OpCodes.Call, registerConstructor);
        }

        private int EmitCreateMethod(TypeDefinition workers, TypeReference type)
        {
            //// [UnmanagedCallersOnly(...)]
            //// public static object Create_Namespace_Class()
            var method = new MethodDefinition(
                "Create_" + type.FullName.Replace('.', '_').Replace('/', '_'),
                Constants.PublicStaticMethod,
                workers.Module.TypeSystem.Object);
            workers.Methods.Add(method);
            CecilHelper.AddUmanagedAttribute(method);

            //// return new Class(...)
            ILProcessor il = method.Body.GetILProcessor();
            this.instanceBuilder.EmitNewObj(type, il);
            il.Emit(OpCodes.Ret);

            CecilHelper.OptimizeBody(method);
            return this.exportedMethods.RegisterMethod("void* {0}()", method.Name);
        }
    }
}
