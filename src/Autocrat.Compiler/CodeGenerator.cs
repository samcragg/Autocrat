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
    using Microsoft.CodeAnalysis.Emit;
    using NLog;

    /// <summary>
    /// Creates the generated code for the managed and native parts of the
    /// application.
    /// </summary>
    internal class CodeGenerator
    {
        private const string NativeCallableAttributeDeclaration = @"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NativeCallableAttribute : Attribute
    {
        public string EntryPoint;
        public CallingConvention CallingConvention;
        public NativeCallableAttribute() { }
    }
}
";

        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly List<SyntaxTree> generatedCode = new List<SyntaxTree>();
        private readonly NativeImportGenerator nativeCode;
        private readonly HashSet<MetadataReference> references = new HashSet<MetadataReference>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerator"/> class.
        /// </summary>
        public CodeGenerator()
        {
            this.nativeCode = ServiceFactory(null).GetNativeImportGenerator();
        }

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
        public virtual void Add(Compilation compilation)
        {
            ServiceFactory factory = ServiceFactory(compilation);

            this.generatedCode.AddRange(compilation.SyntaxTrees);
            this.RewriteInitializers(factory, compilation);
            this.RewriteNativeAdapters(factory);
            this.SaveCompilationMetadata(compilation);
            this.SaveNativeGeneratedCode(factory);
        }

        /// <summary>
        /// Generates the managed assembly.
        /// </summary>
        /// <param name="destination">Where to save the assembly to.</param>
        public virtual void EmitAssembly(Stream destination)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            CSharpCompilation compilation = CSharpCompilation
                .Create("AutocratGeneratedAssembly", options: options)
                .AddReferences(this.references)
                .AddSyntaxTrees(this.generatedCode);

            compilation = EnsureNativeCallableAttributeIsPresent(compilation);
            EmitResult result = compilation.Emit(destination);
            if (!result.Success)
            {
                foreach (Diagnostic diagnostic in result.Diagnostics)
                {
                    Logger.Error(diagnostic.ToString());
                }

                throw new InvalidOperationException("Unable to compile generated code");
            }
        }

        /// <summary>
        /// Generates the native source code.
        /// </summary>
        /// <param name="destination">Where to save the source code to.</param>
        public virtual void EmitNativeCode(Stream destination)
        {
            this.nativeCode.WriteTo(destination);
        }

        private static CSharpCompilation EnsureNativeCallableAttributeIsPresent(CSharpCompilation compilation)
        {
            if (compilation.GetTypeByMetadataName("System.Runtime.InteropServices.NativeCallableAttribute") == null)
            {
                compilation = compilation.AddSyntaxTrees(
                    CSharpSyntaxTree.Create(
                        SyntaxFactory.ParseCompilationUnit(NativeCallableAttributeDeclaration)));
            }

            return compilation;
        }

        private void RewriteInitializers(ServiceFactory factory, Compilation compilation)
        {
            Logger.Info("Rewriting " + nameof(IInitializer) + "s");
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

        private void RewriteNativeAdapters(ServiceFactory factory)
        {
            Logger.Info("Rewriting native adapters");
            SyntaxTreeRewriter rewriter = factory.CreateSyntaxTreeRewriter();
            for (int i = 0; i < this.generatedCode.Count; i++)
            {
                this.generatedCode[i] = rewriter.Generate(this.generatedCode[i]);
            }
        }

        private void SaveCompilationMetadata(Compilation compilation)
        {
            foreach (MetadataReference reference in compilation.References)
            {
                this.references.Add(reference);
            }
        }

        private void SaveNativeGeneratedCode(ServiceFactory factory)
        {
            this.nativeCode.MergeWith(factory.GetNativeImportGenerator());
        }
    }
}
