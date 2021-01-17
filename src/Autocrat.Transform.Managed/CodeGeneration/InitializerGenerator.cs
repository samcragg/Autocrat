// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autocrat.Abstractions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Rewrites code used to initialize the application.
    /// </summary>
    internal class InitializerGenerator
    {
        /// <summary>
        /// Gets the name of the generated class.
        /// </summary>
        internal const string GeneratedClassName = "Initialization";

        // This class generates a static method to invoke the
        // IInitialize.OnInitialize method on classes, so given this:
        //
        //// public class Startup : IInitializer
        //// {
        ////     public Startup(IUdpEvent udpEvent)
        ////     {
        ////         ...
        ////     }
        ////
        ////     public void OnInitialize()
        ////     {
        ////         ...
        ////     }
        //// }
        //
        // then a method like this will be generated:
        //
        //// public class Initialization
        //// {
        ////     [UnmanagedCallersOnly("OnInitialize")]
        ////     public static void OnInitialize()
        ////     {
        ////         var startup = new Startup(new UdpEvent());
        ////         startup.OnInitialize();
        ////     }
        //// }
        //
        // Note that only a single method is generated, so multiple
        // initializers will be created but the order they are invoked in is
        // not specified.
        private readonly InstanceBuilder instanceBuilder;

        private readonly List<(TypeDefinition, MethodDefinition)> methods =
            new List<(TypeDefinition, MethodDefinition)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializerGenerator"/> class.
        /// </summary>
        /// <param name="instanceBuilder">Generates code to create objects.</param>
        public InitializerGenerator(InstanceBuilder instanceBuilder)
        {
            this.instanceBuilder = instanceBuilder;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializerGenerator"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected InitializerGenerator()
        {
            this.instanceBuilder = null!;
        }

        /// <summary>
        /// Registers the class for invoking during initialization.
        /// </summary>
        /// <param name="type">The type information.</param>
        public virtual void AddClass(TypeDefinition type)
        {
            MethodDefinition? method = type.Methods.FirstOrDefault(IsConfigurationMethod);
            if (method != null)
            {
                this.methods.Add((type, method));
            }
        }

        /// <summary>
        /// Emits the generated code to the specified module.
        /// </summary>
        /// <param name="module">Where to emit the new code to.</param>
        public virtual void Emit(ModuleDefinition module)
        {
            //// public sealed class Initialization
            TypeDefinition initializerType = CecilHelper.AddClass(module, GeneratedClassName);

            //// public static void OnConfigurationLoaded()
            var initializeMethod = new MethodDefinition(
                nameof(IInitializer.OnConfigurationLoaded),
                Constants.PublicStaticMethod,
                module.TypeSystem.Void);
            initializerType.Methods.Add(initializeMethod);

            CecilHelper.AddUmanagedAttribute(initializeMethod);
            ILProcessor il = initializeMethod.Body.GetILProcessor();

            //// (new Initializer()).OnConfigurationLoaded
            foreach ((TypeDefinition type, MethodDefinition method) in this.methods)
            {
                this.instanceBuilder.EmitNewObj(type, il);
                il.Emit(OpCodes.Callvirt, method);
            }

            il.Emit(OpCodes.Ret);
            CecilHelper.OptimizeBody(initializeMethod);
        }

        private static bool IsConfigurationMethod(MethodDefinition method)
        {
            static bool IsInterfaceMethod(MethodReference overrideMethod)
            {
                return string.Equals(
                        overrideMethod.Name,
                        nameof(IInitializer.OnConfigurationLoaded),
                        StringComparison.Ordinal) &&
                    string.Equals(
                        overrideMethod.DeclaringType.Name,
                        nameof(IInitializer),
                        StringComparison.Ordinal);
            }

            if (!method.IsVirtual)
            {
                return false;
            }
            else if (method.HasOverrides)
            {
                // Check for explicit interface implementations
                return method.Overrides.Any(IsInterfaceMethod);
            }
            else
            {
                // Check for implicitly implemented methods
                return string.Equals(
                    method.Name,
                    nameof(IInitializer.OnConfigurationLoaded),
                    StringComparison.Ordinal) &&
                    (method.Parameters.Count == 0);
            }
        }
    }
}
