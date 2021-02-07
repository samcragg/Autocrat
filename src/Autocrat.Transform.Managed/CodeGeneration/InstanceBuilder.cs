// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System.Collections.Generic;
    using Autocrat.Common;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Creates expressions for initializing new instances of types.
    /// </summary>
    internal partial class InstanceBuilder
    {
        private readonly ConfigResolver configResolver;
        private readonly ConstructorResolver constructorResolver;
        private readonly InterfaceResolver interfaceResolver;
        private readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceBuilder"/> class.
        /// </summary>
        /// <param name="configResolver">
        /// Used to resolve classes representing configuration data.
        /// </param>
        /// <param name="constructorResolver">
        /// Used to resolve the constructor for a type.
        /// </param>
        /// <param name="interfaceResolver">
        /// Used to resolve concrete types for interface types.
        /// </param>
        public InstanceBuilder(
            ConfigResolver configResolver,
            ConstructorResolver constructorResolver,
            InterfaceResolver interfaceResolver)
        {
            this.configResolver = configResolver;
            this.constructorResolver = constructorResolver;
            this.interfaceResolver = interfaceResolver;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected InstanceBuilder()
        {
            this.configResolver = null!;
            this.constructorResolver = null!;
            this.interfaceResolver = null!;
        }

        /// <summary>
        /// Generates a value of the specified type on the stack.
        /// </summary>
        /// <param name="type">The type of the value to create.</param>
        /// <param name="il">Where to generate the instructions to.</param>
        public virtual void EmitNewObj(TypeReference type, ILProcessor il)
        {
            this.logger.Debug("Emitting code to create an instance of {0}", type.FullName);
            this.EmitNewObj(new EmitContext(type, il));
        }

        private void EmitCreateArray(in EmitContext context, IReadOnlyCollection<TypeReference> types)
        {
            context.Processor.Emit(OpCodes.Ldc_I4, types.Count);
            context.Processor.Emit(OpCodes.Newarr, context.Type.GetElementType());

            int index = 0;
            foreach (TypeReference type in types)
            {
                context.Processor.Emit(OpCodes.Dup);
                context.Processor.Emit(OpCodes.Ldc_I4, index++);
                this.EmitNewObj(new EmitContext(context, type));
                context.Processor.Emit(OpCodes.Stelem_Ref);
            }
        }

        private void EmitNewObj(in EmitContext context)
        {
            MethodDefinition constructor = this.constructorResolver.GetConstructor(context.Type);
            foreach (TypeReference parameter in this.constructorResolver.GetParameters(constructor))
            {
                if (parameter.IsArray)
                {
                    this.EmitCreateArray(
                        new EmitContext(context, parameter),
                        this.interfaceResolver.FindClasses(parameter.GetElementType()));
                }
                else if (!this.configResolver.EmitAccessConfig(parameter, context.Processor))
                {
                    this.EmitNewObj(new EmitContext(context, parameter));
                }
            }

            context.Processor.Emit(OpCodes.Newobj, constructor);
        }
    }
}
