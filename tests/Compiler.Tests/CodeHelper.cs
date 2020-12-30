namespace Compiler.Tests
{
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Autocrat.Abstractions;
    using Autocrat.Compiler.CodeRewriting;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using SR = System.Reflection;

    internal static class CodeHelper
    {
        public static void AssertHasExportedMember(TypeDefinition type, string exportedName)
        {
            static string GetExportedName(MethodDefinition method)
            {
                // All exported methods must be public and static
                if (method.IsPublic && method.IsStatic)
                {
                    CustomAttribute attribute = method.CustomAttributes
                        .FirstOrDefault(a => a.AttributeType.Name == nameof(UnmanagedCallersOnlyAttribute));
                    if (attribute != null)
                    {
                        return attribute.Properties
                            .Single(p => p.Name == nameof(UnmanagedCallersOnlyAttribute.EntryPoint))
                            .Argument.Value.ToString();
                    }
                }

                return null;
            }

            type.Methods.Select(GetExportedName).Should().ContainSingle(exportedName);
        }

        public static ModuleDefinition CompileCode(
            string code,
            bool referenceAbstractions = false)
        {
            string sdkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
            MetadataReference[] references =
            {
                MetadataReference.CreateFromFile(Path.Join(sdkDirectory, "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Join(sdkDirectory, "System.Private.CoreLib.dll")),
                MetadataReference.CreateFromFile(Path.Join(sdkDirectory, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Join(sdkDirectory, "System.Text.Json.dll")),
                MetadataReference.CreateFromFile(typeof(CodeHelper).Assembly.Location),
            };

            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                code,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));

            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release);
            Compilation compilation = CSharpCompilation
                .Create("TestAssembly", options: options)
                .AddReferences(references)
                .AddSyntaxTrees(tree);

            if (referenceAbstractions)
            {
                compilation = AddAbstractionsAssembly(compilation);
            }

            compilation.GetDiagnostics().Should().BeEmpty();

            // NOTE: Don't dispose the stream, as we'll be unable to save the
            // generated assembly if the stream is disposed here
            var stream = new MemoryStream();
            compilation.Emit(stream);

            stream.Position = 0;
            return AssemblyDefinition.ReadAssembly(stream).MainModule;
        }

        public static TypeDefinition CompileType(string classDefinition)
        {
            return CompileCode(classDefinition, referenceAbstractions: true)
                .Types
                .Single(td => td.Name != "<Module>");
        }

        public static SR.Assembly LoadModule(ModuleDefinition module)
        {
            using var stream = new MemoryStream();
            module.Assembly.Write(stream);
            return SR.Assembly.Load(stream.ToArray());
        }

        public static MethodDefinition VisitMethod(
            CilVisitor visitor,
            TypeDefinition type,
            string methodName)
        {
            MethodDefinition method = type.Methods.Single(m => m.Name == methodName);
            foreach (Instruction instruction in method.Body.Instructions.ToArray())
            {
                visitor.Visit(method.Body, instruction);
            }

            return method;
        }

        public static void VisitMethods(
            CilVisitor visitor,
            TypeDefinition type)
        {
            foreach (TypeDefinition nested in type.NestedTypes)
            {
                VisitMethods(visitor, nested);
            }

            foreach (MethodDefinition method in type.Methods)
            {
                foreach (Instruction instruction in method.Body.Instructions.ToArray())
                {
                    visitor.Visit(method.Body, instruction);
                }
            }
        }

        private static Compilation AddAbstractionsAssembly(Compilation compilation)
        {
            return compilation.AddReferences(
                MetadataReference.CreateFromFile(
                    typeof(IInitializer).Assembly.Location));
        }

        public class GeneratedMethod
        {
            private const string MethodName = "TestMethod";
            private const string TypeName = "TestType";
            private readonly TypeDefinition type;

            public GeneratedMethod(ModuleDefinition module = null)
            {
                this.Module = module ?? ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);

                this.type = new TypeDefinition(
                    "",
                    TypeName,
                    TypeAttributes.Public,
                    this.Module.TypeSystem.Object);
                this.Module.Types.Add(type);

                this.Method = new MethodDefinition(
                    MethodName,
                    MethodAttributes.Public | MethodAttributes.Static,
                    this.Module.TypeSystem.Void);
                this.type.Methods.Add(this.Method);

                this.IL = this.Method.Body.GetILProcessor();
            }

            public ILProcessor IL { get; }

            public MethodDefinition Method { get; }

            public ModuleDefinition Module { get; }

            public object GetResult()
            {
                this.Method.ReturnType = this.Module.TypeSystem.Object;
                this.IL.Emit(OpCodes.Ret);
                return this.CreateAndInvokeMethod();
            }

            public void Invoke()
            {
                this.IL.Emit(OpCodes.Ret);
                this.CreateAndInvokeMethod();
            }

            private object CreateAndInvokeMethod()
            {
                SR.Assembly assembly = LoadModule(this.Module);
                SR.MethodInfo method = assembly.GetType(TypeName).GetMethod(MethodName);
                return method.Invoke(null, null);
            }
        }
    }
}
