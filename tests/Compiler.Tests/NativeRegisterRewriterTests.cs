namespace Compiler.Tests
{
    using System;
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using NSubstitute;
    using Xunit;

    public class NativeRegisterRewriterTests
    {
        private const int MethodIndex = 123;
        private const string TestNativeAdapterName =
            "Compiler.Tests." + nameof(NativeRegisterRewriterTests) + "." + nameof(TestNativeAdapter);

        private readonly ManagedCallbackGenerator callbackGenerator;
        private readonly SignatureGenerator signatureGenerator;

        private NativeRegisterRewriterTests()
        {
            this.callbackGenerator = Substitute.For<ManagedCallbackGenerator>(null, null);
            this.callbackGenerator.CreateMethod(null, null)
                .ReturnsForAnyArgs(MethodIndex);

            this.signatureGenerator = Substitute.For<SignatureGenerator>();
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
                this.signatureGenerator);

            rewriter.AddReplacement(
                compilation.GetTypeByMetadataName(interfaceType.FullName),
                compilation.GetTypeByMetadataName(typeof(TestNativeAdapter).FullName));

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
    instance.SingleArgument<MethodsToCall>(other);
}";
                string result = this.VisitCode(typeof(ITestInterface), original);

                result.Should().Contain(@"void Method(ITestInterface instance)
{
    int other = 123;
    " + TestNativeAdapterName + @".InterfaceMethod(other, " + MethodIndex + @");
}");
            }

            [Fact]
            public void ShouldReplaceAllCallsOnTheInterface()
            {
                const string original = @"void Method(ITestInterface instance)
{
    instance.MultipleMethods<MethodsToCall>();
}";

                string result = this.VisitCode(typeof(ITestInterface), original);

                result.Should().Contain(@"void Method(ITestInterface instance)
{
    " + TestNativeAdapterName + @".Method1(" + MethodIndex + @");
    " + TestNativeAdapterName + @".Method2(" + MethodIndex + @");
}");
            }
        }

        private static class TestNativeAdapter
        {
        }

        public class MethodsToCall : ISingleMethod, IMultipleMethods
        {
            public void InterfaceMethod()
            {
            }

            public void Method1()
            {
            }

            public void Method2()
            {
            }
        }
    }
}
