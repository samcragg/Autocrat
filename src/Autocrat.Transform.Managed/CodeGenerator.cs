// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using System.Collections.Generic;
    using System.IO;
    using Autocrat.Abstractions;
    using Autocrat.Common;
    using Autocrat.Transform.Managed.CodeGeneration;
    using Autocrat.Transform.Managed.CodeRewriting;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Creates the generated code for the transformed managed assembly.
    /// </summary>
    internal class CodeGenerator
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly ModuleDefinition module;
        private readonly ServiceFactory serviceFactory;
        private readonly List<TypeReference> workerTypes = new List<TypeReference>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerator"/> class.
        /// </summary>
        /// <param name="factory">Used to create the services.</param>
        /// <param name="module">Contains the module information.</param>
        public CodeGenerator(ServiceFactory factory, ModuleDefinition module)
        {
            this.serviceFactory = factory;
            this.module = module;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerator"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is to make the class easier to be mocked.
        /// </remarks>
        protected CodeGenerator()
        {
            this.serviceFactory = null!;
            this.module = null!;
        }

        /// <summary>
        /// Generates the managed assembly.
        /// </summary>
        /// <param name="destination">Where to save the assembly to.</param>
        /// <param name="pdb">Where to save the debug information.</param>
        public virtual void EmitAssembly(Stream destination, Stream pdb)
        {
            this.serviceFactory.GetKnownTypes().Scan(this.module);

            this.RewriteModule();
            this.EmitConfigurationClass();
            this.EmitInitializer();
            this.EmitManagedClasses();

            var options = new WriterParameters
            {
                SymbolStream = pdb,
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                WriteSymbols = true,
            };
            this.module.Write(destination, options);
        }

        private void EmitConfigurationClass()
        {
            this.logger.Info("Emitting configuration");
            ConfigResolver config = this.serviceFactory.GetConfigResolver();
            ManagedExportsGenerator exports = this.serviceFactory.GetManagedExportsGenerator();

            exports.ConfigClass = config.EmitConfigurationClass(this.module);
        }

        private void EmitInitializer()
        {
            this.logger.Debug("Emitting code for " + nameof(IInitializer) + " classes");
            TypeReference initializerType = this.module.ImportReference(typeof(IInitializer));
            InterfaceResolver interfaceResolver = this.serviceFactory.GetInterfaceResolver();
            InitializerGenerator generator = this.serviceFactory.CreateInitializerGenerator();
            foreach (TypeDefinition type in interfaceResolver.FindClasses(initializerType))
            {
                generator.AddClass(type);
            }

            generator.Emit(this.module);
        }

        private void EmitManagedClasses()
        {
            ManagedExportsGenerator exports =
                this.serviceFactory.GetManagedExportsGenerator();
            ManagedCallbackGenerator managed =
                this.serviceFactory.GetManagedCallbackGenerator();
            WorkerRegisterGenerator worker =
                this.serviceFactory.CreateWorkerRegisterGenerator(this.workerTypes);

            this.logger.Info("Emitting callbacks");
            managed.EmitType(this.module);

            this.logger.Info("Emitting workers");
            exports.WorkersClass = worker.EmitWorkerClass(this.module);

            this.logger.Info("Emitting exports");
            exports.Emit(this.module);
        }

        private void RewriteModule()
        {
            this.logger.Info("Rewriting module");
            WorkerFactoryVisitor workerFactory =
                this.serviceFactory.CreateWorkerFactoryVisitor();

            ModuleRewriter moduleRewriter =
                this.serviceFactory.CreateModuleRewriter();

            moduleRewriter.AddVisitor(workerFactory);
            moduleRewriter.AddVisitor(
                this.serviceFactory.CreateInterfaceRewriter());
            moduleRewriter.AddVisitor(
                this.serviceFactory.CreateNativeDelegateRewriter());

            moduleRewriter.Visit(this.module);

            this.workerTypes.AddRange(workerFactory.WorkerTypes);
        }
    }
}
