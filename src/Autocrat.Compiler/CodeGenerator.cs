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
        private readonly HashSet<MetadataReference> references = new HashSet<MetadataReference>();

        /// <summary>
        /// Gets or sets the function to call to create a
        /// <see cref="ServiceFactory"/>.
        /// </summary>
        internal static Func<Compilation, ServiceFactory> ServiceFactory { get; set; }
            = c => new ServiceFactory(c);

        /// <summary>
        /// Adds code for the specified compilation.
        /// </summary>
        /// <param name="compilation">Contains the compiled information.</param>
        public void Add(Compilation compilation)
        {
            ServiceFactory factory = ServiceFactory(compilation);

            this.generatedCode.AddRange(compilation.SyntaxTrees);
            this.RewriteInitializers(factory, compilation);
            this.SaveCompilationMetadata(compilation);
        }

        /// <summary>
        /// Generates the code for the specified compilation.
        /// </summary>
        /// <param name="destination">Where to save the code to.</param>
        public void Emit(Stream destination)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            CSharpCompilation compilation = CSharpCompilation
                .Create("TestAssembly", options: options)
                .AddReferences(this.references)
                .AddSyntaxTrees(this.generatedCode);

            compilation.Emit(destination);
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

            if (generator.HasCode)
            {
                this.generatedCode.Add(
                    CSharpSyntaxTree.Create(generator.Generate()));
            }
        }

        private void SaveCompilationMetadata(Compilation compilation)
        {
            foreach (MetadataReference reference in compilation.References)
            {
                this.references.Add(reference);
            }
        }
    }
}
