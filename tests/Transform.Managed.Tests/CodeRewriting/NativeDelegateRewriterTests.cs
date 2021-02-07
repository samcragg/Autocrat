namespace Transform.Managed.Tests.CodeRewriting
{
    using Autocrat.Transform.Managed.CodeGeneration;
    using Autocrat.Transform.Managed.CodeRewriting;
    using FluentAssertions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using NSubstitute;
    using Xunit;

    public class NativeDelegateRewriterTests
    {
        private readonly ManagedCallbackGenerator generator;
        private readonly NativeDelegateRewriter rewriter;

        public NativeDelegateRewriterTests()
        {
            this.generator = Substitute.For<ManagedCallbackGenerator>();
            this.rewriter = new NativeDelegateRewriter(this.generator);
        }

        public sealed class VisitTests : NativeDelegateRewriterTests
        {
            [Fact]
            public void ShouldReplaceCreatingADelegateWithLoadingTheHandle()
            {
                this.generator.AddMethod(null, null).ReturnsForAnyArgs(123);
                TypeDefinition testClass = CodeHelper.CompileType(@"
public class TestClass
{
    [Autocrat.Abstractions.NativeDelegate("""")]
    public delegate void TestDelegate();

    public static object CreateDelegate()
    {
        return new TestDelegate(TargetMethod);
    }

    private static void TargetMethod()
    {
    }
}");
                MethodDefinition method =
                    CodeHelper.VisitMethod(this.rewriter, testClass, "CreateDelegate");

                // Need to box the instruction. Normally, this wouldn't be a
                // problem as the delegate is passed to an interface that
                // would be rewritten to one that does accept the correct
                // argument, but for our tests it's easier to just box
                method.Body.Instructions.Insert(
                    method.Body.Instructions.Count - 1,
                    Instruction.Create(OpCodes.Box, testClass.Module.TypeSystem.Int32));

                object result = CodeHelper.LoadModule(testClass.Module)
                    .GetType("TestClass")
                    .GetMethod("CreateDelegate")
                    .Invoke(null, null);

                result.Should().Be(123);
            }
        }
    }
}
