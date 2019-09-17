// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Autocrat.Abstractions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Emit;
    using NLog;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Creates the generated code for the managed and native parts of the
    /// application.
    /// </summary>
    internal class CodeGenerator
    {
        private const string CallbackAdapterClassName = "NativeCallableMethods";
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly List<MethodDeclarationSyntax> callbackMethods = new List<MethodDeclarationSyntax>();
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
        public virtual void Add(Compilation compilation)
        {
            compilation = compilation.AddReferences(
                MetadataReference.CreateFromFile(typeof(CodeGenerator).Assembly.Location));

            ServiceFactory factory = ServiceFactory(compilation);

            this.generatedCode.AddRange(compilation.SyntaxTrees);
            this.RewriteInitializers(factory, compilation);
            this.RewriteNativeAdapters(factory);
            this.SaveCompilationMetadata(compilation);
            this.SaveCallbacks(factory);
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

            compilation = this.AddCallbackAdapters(compilation);

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
            NativeImportGenerator nativeGenerator = ServiceFactory(null).GetNativeImportGenerator();
            nativeGenerator.WriteTo(destination);

            const string MainStub = @"
extern int autocrat_main();

int main()
{
    return autocrat_main();
}";
            byte[] bytes = Encoding.UTF8.GetBytes(MainStub);
            destination.Write(bytes);
        }

        private CSharpCompilation AddCallbackAdapters(CSharpCompilation compilation)
        {
            ClassDeclarationSyntax nativeClass =
                ClassDeclaration(CallbackAdapterClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .WithMembers(List<MemberDeclarationSyntax>(this.callbackMethods));

            UsingDirectiveSyntax nativeInterop = UsingDirective(
                ParseName("System.Runtime.InteropServices"));

            SyntaxTree tree = SyntaxTree(
                CompilationUnit()
                .WithUsings(SingletonList(nativeInterop))
                .WithMembers(SingletonList<MemberDeclarationSyntax>(nativeClass)));

            return compilation.AddSyntaxTrees(tree);
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

        private void SaveCallbacks(ServiceFactory factory)
        {
            ManagedCallbackGenerator callbacks = factory.GetManagedCallbackGenerator();
            this.callbackMethods.AddRange(callbacks.Methods);
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
