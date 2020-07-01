namespace Compiler.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Autocrat.Abstractions;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Emit;

    internal static class CompilationHelper
    {
        public static void AssertExportedAs(MemberDeclarationSyntax member, string expected)
        {
            // All exported methods must be public and static
            member.Modifiers.Select(x => x.ToString())
                  .Should().Contain(new[] { "public", "static" });

            SeparatedSyntaxList<AttributeArgumentSyntax> arguments =
                ((MethodDeclarationSyntax)member).AttributeLists
                .Should().ContainSingle().Which.Attributes.Should().ContainSingle()
                .Subject.ArgumentList.Arguments;

            arguments.Select(x => x.NormalizeWhitespace().ToString())
                .Should().Contain("EntryPoint = \"" + expected + "\"");
        }

        public static Compilation CompileCode(
            string code,
            bool allowErrors = false,
            bool referenceAbstractions = false)
        {
            string sdkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
            MetadataReference[] references =
            {
                MetadataReference.CreateFromFile(Path.Join(sdkDirectory, "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Join(sdkDirectory, "System.Private.CoreLib.dll")),
                MetadataReference.CreateFromFile(Path.Join(sdkDirectory, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Join(sdkDirectory, "System.Text.Json.dll")),
                MetadataReference.CreateFromFile(typeof(CompilationHelper).Assembly.Location),
            };

            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                code,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            Compilation compilation = CSharpCompilation
                .Create("TestAssembly", options: options)
                .AddReferences(references)
                .AddSyntaxTrees(tree);

            if (referenceAbstractions)
            {
                compilation = AddAbstractionsAssembly(compilation);
            }

            if (!allowErrors)
            {
                compilation.GetDiagnostics().Should().BeEmpty();
            }

            return compilation;
        }

        public static IMethodSymbol CreateMethodSymbol(string methodName, string returnType = "void", string arguments = null)
        {
            string returnExpression = returnType == "void" ? string.Empty : "return default;";
            Compilation compilation = CompileCode("partial class TestClass { " +
                returnType + " " + methodName + "(" + arguments + "){" + returnExpression + "}}");
            SyntaxTree tree = compilation.SyntaxTrees.First();
            MethodDeclarationSyntax method = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            SemanticModel model = compilation.GetSemanticModel(tree);
            return model.GetDeclaredSymbol(method);
        }

        public static INamedTypeSymbol CreateTypeSymbol(string classDeclaration)
        {
            Compilation compilation = CompileCode(classDeclaration);
            SyntaxTree tree = compilation.SyntaxTrees.First();
            TypeDeclarationSyntax type = tree.GetRoot()
                .DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .First();

            SemanticModel model = compilation.GetSemanticModel(tree);
            return model.GetDeclaredSymbol(type);
        }

        public static Assembly GenerateAssembly(Compilation compilation)
        {
            using var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);
            result.Success.Should().BeTrue();
            return Assembly.Load(ms.ToArray());
        }

        private static Compilation AddAbstractionsAssembly(Compilation compilation)
        {
            return compilation.AddReferences(
                MetadataReference.CreateFromFile(
                    typeof(IInitializer).Assembly.Location));
        }
    }
}
