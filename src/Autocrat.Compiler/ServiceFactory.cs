﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Creates the services used by the application.
    /// </summary>
    internal class ServiceFactory
    {
        private static readonly Lazy<NativeImportGenerator> NativeImportGenerator = new Lazy<NativeImportGenerator>();
        private readonly Compilation compilation;
        private ConstructorResolver constructorResolver;
        private InterfaceResolver interfaceResolver;
        private IKnownTypes knownTypes;
        private ManagedCallbackGenerator managedCallbackGenerator;

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
                this.CreateNativeRegisterRewriter,
                this.GetKnownTypes());
        }

        /// <summary>
        /// Gets a <see cref="ConstructorResolver"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="ConstructorResolver"/> class.</returns>
        public virtual ConstructorResolver GetConstructorResolver()
        {
            if (this.constructorResolver == null)
            {
                this.constructorResolver = new ConstructorResolver(
                    this.compilation,
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
            if (this.interfaceResolver == null)
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
            if (this.managedCallbackGenerator == null)
            {
                this.managedCallbackGenerator = new ManagedCallbackGenerator(
                    this.CreateInstanceBuilder,
                    this.GetNativeImportGenerator());
            }

            return this.managedCallbackGenerator;
        }

        /// <summary>
        /// Gets a <see cref="NativeImportGenerator"/> instance.
        /// </summary>
        /// <returns>An instance of the <see cref="NativeImportGenerator"/> class.</returns>
        public virtual NativeImportGenerator GetNativeImportGenerator()
        {
            return NativeImportGenerator.Value;
        }

        private NativeRegisterRewriter CreateNativeRegisterRewriter(SemanticModel model)
        {
            return new NativeRegisterRewriter(
                model,
                this.GetManagedCallbackGenerator(),
                new SignatureGenerator());
        }

        private IKnownTypes GetKnownTypes()
        {
            if (this.knownTypes == null)
            {
                var typeVisitor = new NamedTypeVisitor();
                typeVisitor.Visit(this.compilation.GlobalNamespace);
                this.knownTypes = typeVisitor.Types;
            }

            return this.knownTypes;
        }
    }
}
