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
    using Autocrat.Compiler.CodeGeneration;
    using Autocrat.Compiler.Logging;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Emit;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Creates the generated code for the managed and native parts of the
    /// application.
    /// </summary>
    internal class CodeGenerator
    {
        private const string CallbackAdapterClassName = "NativeCallableMethods";
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly List<MethodDeclarationSyntax> callbackMethods = new List<MethodDeclarationSyntax>();
        private readonly List<SyntaxTree> generatedCode = new List<SyntaxTree>();
        private readonly HashSet<MetadataReference> references = new HashSet<MetadataReference>();
        private readonly HashSet<INamedTypeSymbol> workerTypes = new HashSet<INamedTypeSymbol>();

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
            this.SaveWorkerTypes(factory);
        }

        /// <summary>
        /// Generates the managed assembly.
        /// </summary>
        /// <param name="destination">Where to save the assembly to.</param>
        /// <param name="pdb">Where to save the debug information.</param>
        public virtual void EmitAssembly(Stream destination, Stream pdb)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            CSharpCompilation compilation = CSharpCompilation
                .Create("AutocratGeneratedAssembly", options: options)
                .AddReferences(this.references);

            compilation = this.AddCallbackAdapters(compilation);
            compilation = this.AddGeneratedCode(compilation);
            compilation = this.AddRegisterWorkerTypes(compilation);

            EmitResult result = compilation.Emit(destination, pdb);
            if (!result.Success)
            {
                foreach (Diagnostic diagnostic in result.Diagnostics)
                {
                    this.logger.Error(diagnostic.Location, diagnostic.GetMessage());
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
            NativeImportGenerator nativeGenerator = ServiceFactory(null!).GetNativeImportGenerator();
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

        private CSharpCompilation AddGeneratedCode(CSharpCompilation compilation)
        {
            // Fix encoding issue:
            // https://github.com/dotnet/roslyn/issues/24045
            var trees = new SyntaxTree[this.generatedCode.Count];
            for (int i = 0; i < trees.Length; i++)
            {
                SyntaxTree original = this.generatedCode[i];
                trees[i] = CSharpSyntaxTree.Create((CSharpSyntaxNode)original.GetRoot(), encoding: Encoding.UTF8);
            }

            return compilation.AddSyntaxTrees(trees);
        }

        private CSharpCompilation AddRegisterWorkerTypes(CSharpCompilation compilation)
        {
            this.logger.Info("Generating worker types registration");
            ServiceFactory factory = ServiceFactory(compilation);
            WorkerRegisterGenerator generator = factory.CreateWorkerRegisterGenerator(this.workerTypes);
            if (generator.HasCode)
            {
                compilation = compilation.AddSyntaxTrees(
                    CSharpSyntaxTree.Create(generator.Generate(), encoding: Encoding.UTF8));
            }

            return compilation;
        }

        private void RewriteInitializers(ServiceFactory factory, Compilation compilation)
        {
            this.logger.Info("Rewriting " + nameof(IInitializer) + "s");
            INamedTypeSymbol initializer =
                compilation.GetTypeByMetadataName("Autocrat.Abstractions." + nameof(IInitializer))
                ?? throw new InvalidOperationException("Autocrat.Abstractions assembly is not loaded.");

            InterfaceResolver interfaceResolver = factory.GetInterfaceResolver();
            InitializerGenerator generator = factory.CreateInitializerGenerator();
            foreach (INamedTypeSymbol type in interfaceResolver.FindClasses(initializer))
            {
                generator.AddClass(type);
            }

            if (generator.HasCode)
            {
                this.generatedCode.Add(
                    CSharpSyntaxTree.Create(generator.Generate(), encoding: Encoding.UTF8));
            }
        }

        private void RewriteNativeAdapters(ServiceFactory factory)
        {
            this.logger.Info("Rewriting native adapters");
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

        private void SaveWorkerTypes(ServiceFactory factory)
        {
            WorkerFactoryVisitor visitor = factory.CreateWorkerFactoryVisitor();
            this.workerTypes.UnionWith(visitor.WorkerTypes);
        }
    }
}
