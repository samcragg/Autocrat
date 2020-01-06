namespace Compiler.Tests
{
    using System.Linq;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class CompilationHelper
    {
        public static Compilation CompileCode(string code, bool allowErrors = false)
        {
            MetadataReference[] references =
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CompilationHelper).Assembly.Location),
            };

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            CSharpCompilation compilation = CSharpCompilation
                .Create("TestAssembly", options: options)
                .AddReferences(references)
                .AddSyntaxTrees(tree);

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
    }
}
