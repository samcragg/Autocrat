// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using System.Collections.Generic;
    using Autocrat.Transform.Managed.CodeGeneration;
    using Autocrat.Transform.Managed.CodeRewriting;
    using Mono.Cecil;

    /// <summary>
    /// Creates the services used by the application.
    /// </summary>
    internal class ServiceFactory
    {
        private ConfigResolver? configResolver;
        private ConstructorResolver? constructorResolver;
        private ExportedMethods? exportedMethods;
        private InterfaceResolver? interfaceResolver;
        private KnownTypes? knownTypes;
        private ManagedCallbackGenerator? managedCallbackGenerator;
        private ManagedExportsGenerator? managedExportsGenerator;

        /// <summary>
        /// Creates a new <see cref="AssemblyLoader"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="AssemblyLoader"/> class.</returns>
        public virtual AssemblyLoader CreateAssemblyLoader()
        {
            return new AssemblyLoader();
        }

        /// <summary>
        /// Creates a new <see cref="CodeGenerator"/> instance.
        /// </summary>
        /// <param name="module">Contains the module information.</param>
        /// <returns>A new instance of the <see cref="CodeGenerator"/> class.</returns>
        public virtual CodeGenerator CreateCodeGenerator(ModuleDefinition module)
        {
            return new CodeGenerator(this, module);
        }

        /// <summary>
        /// Creates a new <see cref="InitializerGenerator"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="InitializerGenerator"/> class.</returns>
        public virtual InitializerGenerator CreateInitializerGenerator()
        {
            return new InitializerGenerator(
                this.CreateInstanceBuilder());
        }

        /// <summary>
        /// Creates a new <see cref="InterfaceRewriter"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="InterfaceRewriter"/> class.</returns>
        public virtual InterfaceRewriter CreateInterfaceRewriter()
        {
            return new InterfaceRewriter(
                this.GetKnownTypes());
        }

        /// <summary>
        /// Creates a new <see cref="ModuleRewriter"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="ModuleRewriter"/> class.</returns>
        public virtual ModuleRewriter CreateModuleRewriter()
        {
            return new ModuleRewriter();
        }

        /// <summary>
        /// Creates a new <see cref="NativeDelegateRewriter"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="NativeDelegateRewriter"/> class.</returns>
        public virtual NativeDelegateRewriter CreateNativeDelegateRewriter()
        {
            return new NativeDelegateRewriter(
                this.GetManagedCallbackGenerator());
        }

        /// <summary>
        /// Creates a new <see cref="WorkerFactoryVisitor"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="WorkerFactoryVisitor"/> class.</returns>
        public virtual WorkerFactoryVisitor CreateWorkerFactoryVisitor()
        {
            return new WorkerFactoryVisitor();
        }

        /// <summary>
        /// Creates a new <see cref="WorkerRegisterGenerator"/> instance.
        /// </summary>
        /// <param name="factoryTypes">The worker types to register.</param>
        /// <returns>A new instance of the <see cref="WorkerRegisterGenerator"/> class.</returns>
        public virtual WorkerRegisterGenerator CreateWorkerRegisterGenerator(
            IReadOnlyCollection<TypeReference> factoryTypes)
        {
            return new WorkerRegisterGenerator(
                this.CreateInstanceBuilder(),
                factoryTypes,
                this.GetExportedMethods());
        }

        /// <summary>
        /// Gets a <see cref="ConfigResolver"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="ConfigResolver"/> class.</returns>
        public virtual ConfigResolver GetConfigResolver()
        {
            if (this.configResolver is null)
            {
                this.configResolver = new ConfigResolver(
                    this.GetKnownTypes(),
                    CreateConfigGenerator());
            }

            return this.configResolver;
        }

        /// <summary>
        /// Gets a <see cref="ExportedMethods"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="ExportedMethods"/> class.</returns>
        public virtual ExportedMethods GetExportedMethods()
        {
            if (this.exportedMethods is null)
            {
                this.exportedMethods = new ExportedMethods();
            }

            return this.exportedMethods;
        }

        /// <summary>
        /// Gets an <see cref="InterfaceResolver"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="InterfaceResolver"/> class.</returns>
        public virtual InterfaceResolver GetInterfaceResolver()
        {
            if (this.interfaceResolver is null)
            {
                this.interfaceResolver = new InterfaceResolver(
                    this.GetKnownTypes());
            }

            return this.interfaceResolver;
        }

        /// <summary>
        /// Gets a <see cref="KnownTypes"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="KnownTypes"/> class.</returns>
        public virtual KnownTypes GetKnownTypes()
        {
            if (this.knownTypes is null)
            {
                this.knownTypes = new KnownTypes();
            }

            return this.knownTypes;
        }

        /// <summary>
        /// Gets a <see cref="ManagedCallbackGenerator"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="ManagedCallbackGenerator"/> class.</returns>
        public virtual ManagedCallbackGenerator GetManagedCallbackGenerator()
        {
            if (this.managedCallbackGenerator is null)
            {
                this.managedCallbackGenerator = new ManagedCallbackGenerator(
                    this.CreateInstanceBuilder(),
                    this.GetExportedMethods());
            }

            return this.managedCallbackGenerator;
        }

        /// <summary>
        /// Gets a <see cref="ManagedExportsGenerator"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="ManagedExportsGenerator"/> class.</returns>
        public virtual ManagedExportsGenerator GetManagedExportsGenerator()
        {
            if (this.managedExportsGenerator is null)
            {
                this.managedExportsGenerator = new ManagedExportsGenerator();
            }

            return this.managedExportsGenerator;
        }

        private static ConfigGenerator CreateConfigGenerator()
        {
            return new ConfigGenerator();
        }

        private InstanceBuilder CreateInstanceBuilder()
        {
            return new InstanceBuilder(
                this.GetConfigResolver(),
                this.GetConstructorResolver(),
                this.GetInterfaceResolver());
        }

        private ConstructorResolver GetConstructorResolver()
        {
            if (this.constructorResolver is null)
            {
                this.constructorResolver = new ConstructorResolver(
                    this.GetInterfaceResolver());
            }

            return this.constructorResolver;
        }
    }
}
