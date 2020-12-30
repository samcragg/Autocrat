// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Autocrat.Abstractions;
    using Autocrat.Compiler.Logging;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Resolves dependencies representing configuration options.
    /// </summary>
    internal partial class ConfigResolver
    {
        /// <summary>
        /// The name of the generated class.
        /// </summary>
        internal const string ConfigurationClassName = "ApplicationConfiguration";

        /// <summary>
        /// The name of the generated method that reads the configuration.
        /// </summary>
        internal const string ReadConfigurationMethod = "ReadConfig";

        private const string RootProperty = "Root";
        private const string SingletonProperty = "Instance";
        private readonly ConfigGenerator configGenerator;
        private readonly TypeDefinition? configurationClass;

        private readonly Dictionary<string, MethodDefinition?> configurationTypes =
            new Dictionary<string, MethodDefinition?>();

        private readonly ILogger logger = LogManager.GetLogger();
        private MethodDefinition? getInstance;
        private MethodDefinition? getRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigResolver"/> class.
        /// </summary>
        /// <param name="knownTypes">Contains the discovered types.</param>
        /// <param name="configGenerator">Generate the configuration deserializers.</param>
        public ConfigResolver(KnownTypes knownTypes, ConfigGenerator configGenerator)
        {
            this.configGenerator = configGenerator;
            this.configurationClass = FindConfigurationClass(knownTypes);
            if (this.configurationClass != null)
            {
                this.logger.Info("Found configuration class {0}", this.configurationClass.Name);
                this.AddInjectableProperties(this.configurationClass);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigResolver"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected ConfigResolver()
        {
            this.configGenerator = null!;
        }

        /// <summary>
        /// Emits code for accessing the configuration for the specified type.
        /// </summary>
        /// <param name="type">The type to get the configuration for.</param>
        /// <param name="il">Where to emit the instructions to.</param>
        /// <returns>
        /// <c>true</c> if a configuration class was found and, therefore,
        /// code was emitted; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool EmitAccessConfig(TypeReference type, ILProcessor il)
        {
            if (!this.configurationTypes.TryGetValue(type.FullName, out MethodDefinition? getMethod))
            {
                return false;
            }

            Assert(
                !(this.getInstance is null) && !(this.getRoot is null),
                "Must call EmitConfigurationClass first");

            //// Instance.Root(.Property)
            il.Emit(OpCodes.Call, this.getInstance);
            il.Emit(OpCodes.Callvirt, this.getRoot);
            if (getMethod != null)
            {
                il.Emit(OpCodes.Callvirt, getMethod);
            }

            return true;
        }

        /// <summary>
        /// Creates the application configuration class.
        /// </summary>
        /// <param name="module">The module to emit the code to.</param>
        /// <returns>
        /// The class declaration, or <c>null</c> if no configuration class
        /// was found.
        /// </returns>
        public virtual TypeDefinition? EmitConfigurationClass(ModuleDefinition module)
        {
            if (this.configurationClass == null)
            {
                return null;
            }

            TypeDefinition definition = CecilHelper.AddClass(module, ConfigurationClassName);

            var applicationConfig = new GeneratedClass(definition, this.configurationClass);
            MethodDefinition constructor = this.CreateConstructor(applicationConfig);
            this.AddProperties(applicationConfig);
            definition.Methods.Add(constructor);
            definition.Methods.Add(CreateReadMethod(applicationConfig, constructor));
            return definition;
        }

        private static PropertyDefinition AddProperty(
            GeneratedClass context,
            TypeReference type,
            string name,
            MethodAttributes attributes,
            Action<ILProcessor> getMethod,
            Action<ILProcessor>? setMethod)
        {
            static void AddMethod(TypeDefinition declaredType, MethodDefinition definition, Action<ILProcessor> generate)
            {
                generate(definition.Body.GetILProcessor());
                CecilHelper.OptimizeBody(definition);
                declaredType.Methods.Add(definition);
            }

            var property = new PropertyDefinition(name, PropertyAttributes.None, type)
            {
                GetMethod = new MethodDefinition("get_" + name, attributes, type),
            };
            AddMethod(context.Definition, property.GetMethod, getMethod);

            if (setMethod != null)
            {
                property.SetMethod = new MethodDefinition(
                    "set_" + name,
                    attributes,
                    context.Module.TypeSystem.Void);

                property.SetMethod.Parameters.Add(
                    new ParameterDefinition("value", ParameterAttributes.None, type));

                AddMethod(context.Definition, property.SetMethod, setMethod);
            }

            context.Definition.Properties.Add(property);
            return property;
        }

        private static ParameterDefinition CreateReaderParameter(ModuleDefinition module)
        {
            return new ParameterDefinition(
                "reader",
                ParameterAttributes.None,
                module.ImportReference(typeof(Utf8JsonReader).MakeByRefType()));
        }

        private static MethodDefinition CreateReadMethod(
            GeneratedClass context,
            MethodDefinition constructor)
        {
            //// public static void Read(ref Utf8JsonReader reader)
            var method = new MethodDefinition(
                ReadConfigurationMethod,
                Constants.PublicStaticMethod,
                context.Module.TypeSystem.Void);

            ParameterDefinition reader = CreateReaderParameter(context.Module);
            method.Parameters.Add(reader);

            //// Instance = new ApplicationConfiguration(ref reader)
            ILProcessor il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg, 0);
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Stsfld, context.InstanceField);
            il.Emit(OpCodes.Ret);

            return CecilHelper.OptimizeBody(method);
        }

        private static TypeDefinition? FindConfigurationClass(KnownTypes knownTypes)
        {
            static bool IsConfigurationClass(TypeDefinition type)
            {
                return CecilHelper.FindAttribute<ConfigurationAttribute>(type) != null;
            }

            var foundTypes = knownTypes.Where(IsConfigurationClass).ToList();
            return foundTypes.Count switch
            {
                0 => null,
                1 => foundTypes[0],
                _ => throw new InvalidOperationException(
                    "Only a single class can be marked as providing configuration."),
            };
        }

        private void AddInjectableProperties(TypeDefinition type)
        {
            // Add the actual class itself
            this.configurationTypes.Add(type.FullName, null);

            foreach (PropertyDefinition property in type.Properties)
            {
                if (property.PropertyType.MetadataType == MetadataType.Class)
                {
                    this.logger.Info(
                        "Recording configuration property {0} of type {1}",
                        property.Name,
                        property.PropertyType.Name);

                    this.configurationTypes.Add(
                        property.PropertyType.FullName,
                        property.GetMethod);
                }
            }
        }

        private void AddProperties(GeneratedClass context)
        {
            //// public static ApplicationConfiguration Instance { get; set; }
            PropertyDefinition instanceProperty = AddProperty(
                context,
                context.Definition,
                SingletonProperty,
                Constants.PublicStaticMethod,
                il =>
                {
                    il.Emit(OpCodes.Ldsfld, context.InstanceField);
                    il.Emit(OpCodes.Ret);
                },
                il =>
                {
                    il.Emit(OpCodes.Ldarg, 0);
                    il.Emit(OpCodes.Stsfld, context.InstanceField);
                    il.Emit(OpCodes.Ret);
                });

            this.getInstance = instanceProperty.GetMethod;

            //// public ConfigType Root { get; }
            PropertyDefinition rootProperty = AddProperty(
                context,
                context.ConfigurationClass,
                RootProperty,
                Constants.PublicMethod,
                il =>
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, context.RootField);
                    il.Emit(OpCodes.Ret);
                },
                null);

            this.getRoot = rootProperty.GetMethod;
        }

        private MethodDefinition CreateConstructor(GeneratedClass context)
        {
            Assert(this.configurationClass != null, "configurationClass must be set before calling this method");
            TypeDefinition deserializerType =
                this.configGenerator.GetClassFor(context.ConfigurationClass);

            MethodDefinition deserializerCtor =
                deserializerType.Methods.Single(m => m.IsConstructor);

            MethodDefinition deserializerRead =
                deserializerType.Methods.Single(m =>
                    string.Equals(JsonDeserializerBuilder.ReadMethodName, m.Name, StringComparison.Ordinal));

            //// public ApplicationConfiguration(ref Utf8JsonReader reader)
            var ctor = new MethodDefinition(
                Constants.Constructor,
                Constants.PublicConstructor,
                context.Module.TypeSystem.Void);

            ParameterDefinition reader = CreateReaderParameter(context.Module);
            ctor.Parameters.Add(reader);

            //// object()
            ILProcessor il = ctor.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, CecilHelper.ObjectConstructor(context.Module));

            ////     this.root = new MyConfigDeserializer().Read(ref reader)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, deserializerCtor);
            il.Emit(OpCodes.Ldarg, 1);
            il.Emit(OpCodes.Callvirt, deserializerRead);
            il.Emit(OpCodes.Stfld, context.RootField);

            il.Emit(OpCodes.Ret);
            return CecilHelper.OptimizeBody(ctor);
        }
    }
}
