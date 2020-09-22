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
    using Autocrat.NativeAdapters;
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
        private readonly List<MethodDeclarationSyntax> callbackMethods = new List<MethodDeclarationSyntax>();
        private readonly List<SyntaxTree> generatedCode = new List<SyntaxTree>();
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly HashSet<MetadataReference> references = new HashSet<MetadataReference>();
        private readonly ServiceFactory serviceFactory;
        private readonly Compilation source;
        private readonly HashSet<INamedTypeSymbol> workerTypes = new HashSet<INamedTypeSymbol>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerator"/> class.
        /// </summary>
        /// <param name="factory">Used to create the services.</param>
        /// <param name="compilation">Contains the compiled information.</param>
        public CodeGenerator(ServiceFactory factory, Compilation compilation)
        {
            this.serviceFactory = factory;
            this.source = compilation;

            this.generatedCode.AddRange(compilation.SyntaxTrees);
            this.AddConfiguration();
            this.RewriteInitializers();
            this.RewriteNativeAdapters();
            this.SaveCompilationMetadata();
            this.SaveCallbacks();
            this.SaveWorkerTypes();
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
            this.source = null!;
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
                .AddReferences(this.references)
                .AddSyntaxTrees(this.generatedCode);

            compilation = this.AddCallbackAdapters(compilation);
            compilation = this.AddRegisterWorkerTypes(compilation);
            compilation = this.AddManagedExports(compilation);

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
        /// <param name="version">The version information to report at runtime.</param>
        /// <param name="description">The description to report at runtime.</param>
        /// <param name="destination">Where to save the source code to.</param>
        public virtual void EmitNativeCode(string? version, string? description, Stream destination)
        {
            NativeImportGenerator nativeGenerator = this.serviceFactory.GetNativeImportGenerator();
            nativeGenerator.WriteTo(destination);

            string mainMethod = CreateNativeMain(version, description);
            byte[] bytes = Encoding.UTF8.GetBytes(mainMethod);
            destination.Write(bytes);
        }

        private static CSharpCompilation AddSyntaxTree(CSharpCompilation compilation, MethodGenerator generator)
        {
            if (generator.HasCode)
            {
                return compilation.AddSyntaxTrees(
                    CSharpSyntaxTree.Create(generator.Generate(), encoding: Encoding.UTF8));
            }
            else
            {
                return compilation;
            }
        }

        private static string CreateNativeMain(string? version, string? description)
        {
            var buffer = new StringBuilder();
            buffer.AppendLine("int main(int argc, char* argv[])");
            buffer.AppendLine("{");

            if (!string.IsNullOrEmpty(description))
            {
                buffer.Append("    set_description(\"").Append(description).AppendLine("\");");
            }

            if (!string.IsNullOrEmpty(version))
            {
                buffer.Append("    set_version(\"").Append(version).AppendLine("\");");
            }

            buffer.AppendLine("    return autocrat_main(argc, argv);");
            buffer.AppendLine("}");
            return buffer.ToString();
        }

        private static IEnumerable<MetadataReference> GetAutocratReferences()
        {
            yield return MetadataReference.CreateFromFile(typeof(ConfigurationAttribute).Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(ConfigService).Assembly.Location);
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

        private void AddConfiguration()
        {
            ClassDeclarationSyntax? configClass = this.serviceFactory
                .GetConfigResolver()
                .CreateConfigurationClass();

            if (!(configClass is null))
            {
                this.serviceFactory.GetManagedExportsGenerator().IncludeConfig = true;

                ConfigGenerator generator = this.serviceFactory.GetConfigGenerator();
                CompilationUnitSyntax compilation =
                    generator.Generate()
                             .AddMembers(configClass);

                this.generatedCode.Add(SyntaxTree(compilation));
            }
        }

        private CSharpCompilation AddManagedExports(CSharpCompilation compilation)
        {
            this.logger.Info("Generating managed type exports");
            return AddSyntaxTree(
                compilation,
                this.serviceFactory.GetManagedExportsGenerator());
        }

        private CSharpCompilation AddRegisterWorkerTypes(CSharpCompilation compilation)
        {
            this.logger.Info("Generating worker types registration");
            return AddSyntaxTree(
                compilation,
                this.serviceFactory.CreateWorkerRegisterGenerator(this.workerTypes));
        }

        private void RewriteInitializers()
        {
            this.logger.Info("Rewriting " + nameof(IInitializer) + "s");
            INamedTypeSymbol initializer =
                this.source.GetTypeByMetadataName("Autocrat.Abstractions." + nameof(IInitializer))
                ?? throw new InvalidOperationException("Autocrat.Abstractions assembly is not loaded.");

            InterfaceResolver interfaceResolver = this.serviceFactory.GetInterfaceResolver();
            InitializerGenerator generator = this.serviceFactory.CreateInitializerGenerator();
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

        private void RewriteNativeAdapters()
        {
            this.logger.Info("Rewriting native adapters");
            SyntaxTreeRewriter rewriter = this.serviceFactory.CreateSyntaxTreeRewriter();
            for (int i = 0; i < this.generatedCode.Count; i++)
            {
                this.generatedCode[i] = rewriter.Generate(this.generatedCode[i]);
            }
        }

        private void SaveCallbacks()
        {
            ManagedCallbackGenerator callbacks = this.serviceFactory.GetManagedCallbackGenerator();
            this.callbackMethods.AddRange(callbacks.Methods);
        }

        private void SaveCompilationMetadata()
        {
            this.references.UnionWith(GetAutocratReferences());
            foreach (MetadataReference reference in this.source.References)
            {
                this.references.Add(reference);
            }
        }

        private void SaveWorkerTypes()
        {
            WorkerFactoryVisitor visitor = this.serviceFactory.CreateWorkerFactoryVisitor();
            this.workerTypes.UnionWith(visitor.WorkerTypes);
        }
    }
}
