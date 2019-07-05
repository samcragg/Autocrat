namespace Compiler.Tests
{
    using System;
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NSubstitute;
    using Xunit;

    public class NativeRegisterRewriterTests
    {
        private const string TestNativeAdapterName =
            "Compiler.Tests." + nameof(NativeRegisterRewriterTests) + "." + nameof(TestNativeAdapter);

        private readonly ManagedCallbackGenerator callbackGenerator;

        private NativeRegisterRewriterTests()
        {
            this.callbackGenerator = Substitute.For<ManagedCallbackGenerator>();
            this.callbackGenerator.CreateMethod(null)
                .ReturnsForAnyArgs(ci => SyntaxFactory.ParseName("Callback_" + ci.Arg<IMethodSymbol>().Name));
        }

        public interface IMultipleMethods
        {
            void Method1();

            void Method2();
        }

        public interface ISingleMethod
        {
            void InterfaceMethod();
        }

        public interface ITestInterface
        {
            void MultipleMethods<T>() where T : IMultipleMethods;

            void SingleArgument<T>(int value) where T : ISingleMethod;
        }

        private string VisitCode(Type interfaceType, string code)
        {
            Compilation compilation = CompilationHelper.CompileCode(@"
using static Compiler.Tests.NativeRegisterRewriterTests;
class TestClass{ " + code + "}");

            SyntaxTree tree = compilation.SyntaxTrees.First();
            SemanticModel model = compilation.GetSemanticModel(tree);

            var rewriter = new NativeRegisterRewriter(
                model,
                this.callbackGenerator,
                interfaceType,
                typeof(TestNativeAdapter));

            return rewriter.Visit(tree.GetRoot()).ToFullString();
        }

        public class VisitTests : NativeRegisterRewriterTests
        {
            [Fact]
            public void ShouldIgnoreOtherInterfacesPassedInTheMethod()
            {
                const string original = @"void Method(System.IDisposable instance)
{
    instance.Dispose();
}";

                string result = this.VisitCode(typeof(ICloneable), original);

                result.Should().Contain(original);
            }

            [Fact]
            public void ShouldIncludeTheOriginalArgumentsWhenReplacingTheCall()
            {
                const string original = @"void Method(ITestInterface instance)
{
    int other = 123;
    instance.SingleArgument<TestClass>(other);
}";
                string result = this.VisitCode(typeof(ITestInterface), original);

                result.Should().Contain(@"void Method(ITestInterface instance)
{
    int other = 123;
    " + TestNativeAdapterName + @".InterfaceMethod(other, ""Callback_InterfaceMethod"");
}");
            }

            [Fact]
            public void ShouldReplaceAllCallsOnTheInterface()
            {
                const string original = @"void Method(ITestInterface instance)
{
    instance.MultipleMethods<TestClass>();
}";

                string result = this.VisitCode(typeof(ITestInterface), original);

                result.Should().Contain(@"void Method(ITestInterface instance)
{
    " + TestNativeAdapterName + @".Method1(""Callback_Method1"");
    " + TestNativeAdapterName + @".Method2(""Callback_Method2"");
}");
            }
        }

        private static class TestNativeAdapter
        {
        }
    }
}
