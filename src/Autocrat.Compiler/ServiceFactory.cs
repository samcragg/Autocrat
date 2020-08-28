// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using Autocrat.Compiler.CodeGeneration;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Creates the services used by the application.
    /// </summary>
    internal class ServiceFactory
    {
        private static readonly Lazy<NativeImportGenerator> NativeImportGenerator = new Lazy<NativeImportGenerator>();
        private readonly Compilation compilation;
        private ConfigGenerator? configGenerator;
        private ConfigResolver? configResolver;
        private ConstructorResolver? constructorResolver;
        private InterfaceResolver? interfaceResolver;
        private IKnownTypes? knownTypes;
        private ManagedCallbackGenerator? managedCallbackGenerator;
        private ManagedExportsGenerator? managedExportsGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFactory"/> class.
        /// </summary>
        /// <param name="compilation">Contains the compiled information.</param>
        public ServiceFactory(Compilation compilation)
        {
            this.compilation = compilation;
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
        /// Creates a new <see cref="InstanceBuilder"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="InstanceBuilder"/> class.</returns>
        public virtual InstanceBuilder CreateInstanceBuilder()
        {
            return new InstanceBuilder(
                this.GetConstructorResolver(),
                this.GetInterfaceResolver());
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxTreeRewriter"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="SyntaxTreeRewriter"/> class.</returns>
        public virtual SyntaxTreeRewriter CreateSyntaxTreeRewriter()
        {
            return new SyntaxTreeRewriter(
                this.compilation,
                this.CreateInterfaceRegisterRewriter);
        }

        /// <summary>
        /// Creates a new <see cref="WorkerFactoryVisitor"/> instance.
        /// </summary>
        /// <returns>A new instance of the <see cref="WorkerFactoryVisitor"/> class.</returns>
        public virtual WorkerFactoryVisitor CreateWorkerFactoryVisitor()
        {
            return new WorkerFactoryVisitor(this.compilation);
        }

        /// <summary>
        /// Creates a new <see cref="WorkerRegisterGenerator"/> instance.
        /// </summary>
        /// <param name="factoryTypes">The worker types to register.</param>
        /// <returns>A new instance of the <see cref="WorkerRegisterGenerator"/> class.</returns>
        public virtual WorkerRegisterGenerator CreateWorkerRegisterGenerator(
            IReadOnlyCollection<INamedTypeSymbol> factoryTypes)
        {
            return new WorkerRegisterGenerator(
                this.CreateInstanceBuilder,
                factoryTypes,
                this.GetNativeImportGenerator());
        }

        /// <summary>
        /// Gets a <see cref="ConfigGenerator"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="ConfigGenerator"/> class.</returns>
        public virtual ConfigGenerator GetConfigGenerator()
        {
            if (this.configGenerator is null)
            {
                this.configGenerator = new ConfigGenerator();
            }

            return this.configGenerator;
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
                    this.GetConfigGenerator());
            }

            return this.configResolver;
        }

        /// <summary>
        /// Gets a <see cref="ConstructorResolver"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="ConstructorResolver"/> class.</returns>
        public virtual ConstructorResolver GetConstructorResolver()
        {
            if (this.constructorResolver is null)
            {
                this.constructorResolver = new ConstructorResolver(
                    this.compilation,
                    this.GetKnownTypes(),
                    this.GetConfigResolver(),
                    this.GetInterfaceResolver());
            }

            return this.constructorResolver;
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
        /// Gets a <see cref="ManagedCallbackGenerator"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="ManagedCallbackGenerator"/> class.</returns>
        public virtual ManagedCallbackGenerator GetManagedCallbackGenerator()
        {
            if (this.managedCallbackGenerator is null)
            {
                this.managedCallbackGenerator = new ManagedCallbackGenerator(
                    this.CreateInstanceBuilder,
                    this.GetNativeImportGenerator());
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

        /// <summary>
        /// Gets a <see cref="NativeImportGenerator"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="NativeImportGenerator"/> class.</returns>
        public virtual NativeImportGenerator GetNativeImportGenerator()
        {
            return NativeImportGenerator.Value;
        }

        private InterfaceRewriter CreateInterfaceRegisterRewriter(SemanticModel model)
        {
            return new InterfaceRewriter(
                model,
                this.GetKnownTypes(),
                new NativeDelegateRewriter(
                    this.GetManagedCallbackGenerator(),
                    model));
        }

        private IKnownTypes GetKnownTypes()
        {
            if (this.knownTypes is null)
            {
                var typeVisitor = new NamedTypeVisitor();
                typeVisitor.Visit(this.compilation.GlobalNamespace);
                this.knownTypes = typeVisitor.Types;
            }

            return this.knownTypes;
        }
    }
}
