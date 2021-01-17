namespace Transform.Managed.Tests.CodeRewriting
{
    using Autocrat.Transform.Managed;
    using Autocrat.Transform.Managed.CodeRewriting;
    using FluentAssertions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using NSubstitute;
    using Xunit;

    public class InterfaceRewriterTests
    {
        private readonly TypeDefinition implementingClass;
        private readonly TypeDefinition interfaceType;
        private readonly KnownTypes knownTypes;
        private readonly ModuleDefinition module;
        private readonly InterfaceRewriter rewriter;
        private readonly TypeDefinition testClass;

        private InterfaceRewriterTests()
        {
            this.module = CodeHelper.CompileCode(@"
public interface ITestInterface
{
    int GetValue();
}

public static class ImplementingClass
{
    public static int GetValue()
    {
        return 123;
    }
}

public class TestClass
{
    private ITestInterface testInterface;

    public void StoreField(ITestInterface instance)
    {
        this.testInterface = instance;
    }

    public static int InvokeGetValue(ITestInterface instance)
    {
        return instance.GetValue();
    }
}
");
            this.interfaceType = this.module.GetType("ITestInterface");
            this.implementingClass = this.module.GetType("ImplementingClass");
            this.testClass = this.module.GetType("TestClass");

            this.knownTypes = Substitute.For<KnownTypes>();
            this.knownTypes.ShouldRewrite(this.interfaceType)
                .Returns(true);

            this.knownTypes.FindClassForInterface(this.interfaceType)
                .Returns(this.implementingClass);

            this.rewriter = new InterfaceRewriter(this.knownTypes);
        }

        public sealed class VisitTests : InterfaceRewriterTests
        {
            [Fact]
            public void ShouldRewriteMethodCalls()
            {
                CodeHelper.VisitMethod(this.rewriter, this.testClass, "InvokeGetValue");

                object result = CodeHelper.LoadModule(this.module)
                    .GetType("TestClass")
                    .GetMethod("InvokeGetValue")
                    .Invoke(null, new object[] { null });
                result.Should().Be(123);
            }

            [Fact]
            public void ShouldRewriteStoringOfInterfaces()
            {
                MethodDefinition method =
                    CodeHelper.VisitMethod(this.rewriter, this.testClass, "StoreField");

                method.Body.Instructions.Should().ContainSingle()
                    .Which.OpCode.Code.Should().Be(Code.Ret);
            }
        }
    }
}
