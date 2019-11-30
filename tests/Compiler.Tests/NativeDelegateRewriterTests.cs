namespace Compiler.Tests
{
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NSubstitute;
    using Xunit;

    public class NativeDelegateRewriterTests
    {
        private readonly ManagedCallbackGenerator callbackGenerator = Substitute.For<ManagedCallbackGenerator>();

        private ArgumentSyntax VisitCode(string code)
        {
            Compilation compilation = CompilationHelper.CompileCode(@"
namespace Autocrat.Abstractions
{
    public sealed class NativeDelegateAttribute : System.Attribute
    {
        public NativeDelegateAttribute(string s) { }
    }
}

[Autocrat.Abstractions.NativeDelegate(""native_signature"")]
delegate void TestDelegate(int p);

class TestClass
{
    void TestDelegateMethod(int p) { }
    void AcceptsNativeDelegate(TestDelegate action) { }
    void AcceptsNormalDelegate(System.Action<int> action) { }
    void TestMethod()
    {
" + code +
@"}
}");

            SyntaxTree tree = compilation.SyntaxTrees.First();
            SemanticModel model = compilation.GetSemanticModel(tree);

            var rewriter = new NativeDelegateRewriter(
                this.callbackGenerator,
                model);

            return tree.GetRoot().DescendantNodes()
                       .OfType<ArgumentSyntax>()
                       .Select(rewriter.TransformArgument)
                       .Single();
        }

        public sealed class TransformArgumentTests : NativeDelegateRewriterTests
        {
            [Fact]
            public void ShouldChangeIdentifierArguments()
            {
                this.callbackGenerator.CreateMethod(null, null)
                    .ReturnsForAnyArgs(123);

                ArgumentSyntax result = this.VisitCode("AcceptsNativeDelegate(TestDelegateMethod)");

                result.ToString().Should().Be("123");
            }

            [Fact]
            public void ShouldChangeSimpleMemberAccessArguments()
            {
                this.callbackGenerator.CreateMethod(null, null)
                    .ReturnsForAnyArgs(123);

                ArgumentSyntax result = this.VisitCode("AcceptsNativeDelegate(this.TestDelegateMethod)");

                result.ToString().Should().Be("123");
            }

            [Fact]
            public void ShouldIgnoreNonDelegateArguments()
            {
                ArgumentSyntax result = this.VisitCode("TestDelegateMethod(123)");

                result.ToString().Should().Be("123");
            }

            [Fact]
            public void ShouldIgnoreNonNativeDelegateArguments()
            {
                ArgumentSyntax result = this.VisitCode("AcceptsNormalDelegate(this.TestDelegateMethod)");

                result.ToString().Should().Be("this.TestDelegateMethod");
            }

            [Fact]
            public void ShouldPassInTheNativeSignature()
            {
                this.VisitCode("AcceptsNativeDelegate(this.TestDelegateMethod)");

                this.callbackGenerator.Received()
                    .CreateMethod("native_signature", Arg.Any<IMethodSymbol>());
            }
        }
    }
}
