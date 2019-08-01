// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Autocrat.Abstractions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    /// <summary>
    /// Creates the generated code for the managed and native parts of the
    /// application.
    /// </summary>
    internal class CodeGenerator
    {
        private readonly List<SyntaxTree> generatedCode = new List<SyntaxTree>();

        /// <summary>
        /// Gets or sets the function to call to create a
        /// <see cref="ServiceFactory"/>.
        /// </summary>
        internal static Func<Compilation, ServiceFactory> ServiceFactory { get; set; }
            = c => new ServiceFactory(c);

        /// <summary>
        /// Generates the code for the specified compilation.
        /// </summary>
        /// <param name="destination">Where to save the code to.</param>
        /// <param name="compilation">Contains the compiled information.</param>
        public void Emit(Stream destination, Compilation compilation)
        {
            ServiceFactory factory = ServiceFactory(compilation);

            this.generatedCode.AddRange(compilation.SyntaxTrees);
            this.RewriteInitializers(factory, compilation);

            this.Compile(compilation)
                .Emit(destination);
        }

        private Compilation Compile(Compilation compilation)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            return CSharpCompilation
                .Create("TestAssembly", options: options)
                .AddReferences(compilation.References)
                .AddSyntaxTrees(this.generatedCode);
        }

        private void RewriteInitializers(ServiceFactory factory, Compilation compilation)
        {
            INamedTypeSymbol initializer = compilation.GetTypeByMetadataName(
                "Autocrat.Abstractions." + nameof(IInitializer));

            InterfaceResolver interfaceResolver = factory.GetInterfaceResolver();
            InitializerGenerator generator = factory.CreateInitializerGenerator();
            foreach (INamedTypeSymbol type in interfaceResolver.FindClasses(initializer))
            {
                generator.AddClass(type);
            }

            this.generatedCode.Add(
                CSharpSyntaxTree.Create(generator.Generate()));
        }
    }
}
