namespace Transform.Managed.Tests.CodeRewriting
{
    using System;
    using Autocrat.Transform.Managed.CodeRewriting;
    using FluentAssertions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Collections.Generic;
    using NSubstitute;
    using Xunit;

    public class ModuleRewriterTests
    {
        private readonly ModuleRewriter rewriter;
        private readonly CilVisitor visitor;

        private ModuleRewriterTests()
        {
            this.visitor = Substitute.For<CilVisitor>();

            this.rewriter = new ModuleRewriter();
            this.rewriter.AddVisitor(this.visitor);
        }

        public sealed class VisitTests : ModuleRewriterTests
        {
            [Fact]
            public void ShouldAllowModificationOfInstructions()
            {
                this.visitor.When(x => x.Visit(
                    Arg.Is<MethodBody>(m => m.Method.Name == "TestMethod"),
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Ldc_I4_S)))
                    .Do(ci =>
                    {
                        Collection<Instruction> instructions = ci.Arg<MethodBody>().Instructions;
                        instructions.RemoveAt(0);
                        instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)123));
                    });

                ModuleDefinition module = CodeHelper.CompileCode(@"public static class TestClass
{
    public static int TestMethod()
    {
        return 12;
    }
}");

                this.rewriter.Visit(module);
                Type testClass = CodeHelper.LoadModule(module).GetType("TestClass");

                testClass.GetMethod("TestMethod").Invoke(null, null)
                         .Should().Be(123);
            }

            [Fact]
            public void ShouldVisitAllTheInstructionsInAMethod()
            {
                this.rewriter.Visit(CodeHelper.CompileCode(@"class TestClass
{
    public int TestMethod()
    {
        return 123;
    }
}"));

                this.visitor.Visit(
                    Arg.Is<MethodBody>(m => m.Method.Name == "TestMethod"),
                    Arg.Is<Instruction>(i => (i.OpCode.Code == Code.Ldc_I4_S) && ((int)i.Operand == 123)));

                this.visitor.Visit(
                    Arg.Is<MethodBody>(m => m.Method.Name == "TestMethod"),
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Ret));
            }

            [Fact]
            public void ShouldVisitAllTheVisitors()
            {
                CilVisitor additionalVicitor = Substitute.For<CilVisitor>();
                this.rewriter.AddVisitor(additionalVicitor);

                this.rewriter.Visit(CodeHelper.CompileCode(@"class TestClass
{
    public void Method()
    {
    }
}"));

                this.visitor.ReceivedWithAnyArgs().Visit(null, null);
                additionalVicitor.ReceivedWithAnyArgs().Visit(null, null);
            }
        }
    }
}
