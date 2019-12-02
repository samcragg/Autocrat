namespace Compiler.Tests
{
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    public class InterfaceRewriterTests
    {
        private readonly NativeDelegateRewriter delegateRewriter;
        private readonly InterfaceRewriter interfaceRewriter;
        private readonly INamedTypeSymbol replacementClass;
        private readonly MethodDeclarationSyntax testMethod;

        private InterfaceRewriterTests()
        {
            this.delegateRewriter = Substitute.For<NativeDelegateRewriter>();
            this.delegateRewriter.TransformArgument(null).ReturnsForAnyArgs(ci => ci.Arg<ArgumentSyntax>());

            Compilation compilation = CompilationHelper.CompileCode(@"
namespace Autocrat.Abstractions
{
    public sealed class RewriteInterfaceAttribute : System.Attribute
    {
        public RewriteInterfaceAttribute(System.Type interfaceType) { }
    }
}

interface IFakeInterface
{
    void InterfaceMethod(int a);
}

[Autocrat.Abstractions.RewriteInterface(typeof(IFakeInterface))]
static class ReplacementClass
{
    public static void InterfaceMethod(int a)
    {
    }
}

class TestClass
{
    void TestMethod(IFakeInterface instance)
    {
        instance.InterfaceMethod(123);
    }
}");

            SyntaxTree tree = compilation.SyntaxTrees.Single();
            this.replacementClass = compilation.GetTypeByMetadataName("ReplacementClass");
            this.testMethod = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.ToString() == "TestMethod")
                .First();

            this.interfaceRewriter = new InterfaceRewriter(
                compilation.GetSemanticModel(tree),
                this.delegateRewriter);
        }

        public sealed class VisitTests : InterfaceRewriterTests
        {
            [Fact]
            public void ShouldRewriteArguments()
            {
                this.delegateRewriter.TransformArgument(null)
                    .ReturnsForAnyArgs(Argument(LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        Literal(456))));

                this.interfaceRewriter.RegisterClass(this.replacementClass);
                SyntaxNode result = this.interfaceRewriter.Visit(this.testMethod);

                result.ToString().Should().Contain("InterfaceMethod(456)");
            }

            [Fact]
            public void ShouldRewriteRegisteredClasses()
            {
                this.interfaceRewriter.RegisterClass(this.replacementClass);
                SyntaxNode result = this.interfaceRewriter.Visit(this.testMethod);

                result.ToString().Should().NotContain("instance.InterfaceMethod");
                result.ToString().Should().Contain("ReplacementClass.InterfaceMethod");
            }
        }
    }
}
