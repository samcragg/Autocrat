namespace Compiler.Tests.CodeRewriting
{
    using System;
    using Autocrat.Compiler.CodeRewriting;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using NSubstitute;
    using Xunit;

    public class CilVisitorTests
    {
        private void Visit(CilVisitor visitor, string classDefinition)
        {
            TypeDefinition generated = CodeHelper.CompileType(classDefinition);
            foreach (MethodDefinition method in generated.Methods)
            {
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    visitor.Visit(method.Body, instruction);
                }
            }
        }

        public sealed class OnLoadValueTests : CilVisitorTests
        {
            private readonly FakeCilVisitor visitor = Substitute.ForPartsOf<FakeCilVisitor>();

            [Fact]
            public void ShouldHandleLoadArgumentsAtConstantIndexes()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public static int LdArgConstant(object a, int b)
    {
        return b;
    }
}");

                this.visitor.Received().LoadValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Ldarg_1),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleLoadArgumentsWithShortIndexes()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public int LdArgShort(object a, object b, object c, object d, int e)
    {
        return e;
    }
}");

                this.visitor.Received().LoadValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Ldarg_S),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleLoadInstanceFields()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    private int instanceField = 1;
    public int LdFldInstance()
    {
        return this.instanceField;
    }
}");

                this.visitor.Received().LoadValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Ldfld),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleLoadLocalsAtConstantIndexes()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public int LdLocConstant()
    {
        int a;
        UseValue(out a);
        return a;
    }

    private static void UseValue(out int value)
    {
        value = 1;
    }
}");

                this.visitor.Received().LoadValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Ldloc_0),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleLoadLocalsWithShortIndexes()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public int LdLocShort()
    {
        int a, b, c, d, e;
        UseValue(out a);
        UseValue(out b);
        UseValue(out c);
        UseValue(out d);
        UseValue(out e);
        return e;
    }

    private static void UseValue(out int value)
    {
        value = 1;
    }
}");

                this.visitor.Received().LoadValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Ldloc_S),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleLoadStaticFields()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    private static int staticField = 1;
    public static int LdFldStatic()
    {
        return staticField;
    }
}");

                this.visitor.Received().LoadValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Ldsfld),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            internal class FakeCilVisitor : CilVisitor
            {
                public virtual void LoadValue(Instruction instruction, TypeReference type)
                {
                }

                protected override void OnLoadValue(Instruction instruction, TypeReference type)
                {
                    this.LoadValue(instruction, type);
                }
            }
        }

        public sealed class OnMethodCallTests : CilVisitorTests
        {
            private readonly FakeCilVisitor visitor = Substitute.ForPartsOf<FakeCilVisitor>();

            [Fact]
            public void ShouldHandleCallStaticMethods()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public int CallStatic()
    {
        return StaticMethod();
    }

    private static int StaticMethod()
    {
        return 1;
    }
}");

                this.visitor.Received().MethodCall(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Call),
                    Arg.Is<MethodReference>(m => m.Name == "StaticMethod"));
            }

            [Fact]
            public void ShouldHandleCallVirtualMethods()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public int CallVirtual()
    {
        return this.VirtualMethod();
    }

    protected virtual int VirtualMethod()
    {
        return 1;
    }
}");

                this.visitor.Received().MethodCall(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Callvirt),
                    Arg.Is<MethodReference>(m => m.Name == "VirtualMethod"));
            }

            internal class FakeCilVisitor : CilVisitor
            {
                public virtual void MethodCall(Instruction instruction, MethodReference method)
                {
                }

                protected override void OnMethodCall(Instruction instruction, MethodReference method)
                {
                    this.MethodCall(instruction, method);
                }
            }
        }

        public sealed class OnStoreValueTests : CilVisitorTests
        {
            private readonly FakeCilVisitor visitor = Substitute.ForPartsOf<FakeCilVisitor>();

            [Fact]
            public void ShouldHandleStoreArguments()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public void StArg(object a, int b)
    {
        b = 1;
    }
}");

                this.visitor.Received().StoreValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Starg_S),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleStoreInstanceFields()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    private int instanceField;
    public void StFldInstance()
    {
        this.instanceField += 1;
    }
}");

                this.visitor.Received().StoreValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Stfld),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleStoreLocalsAtConstantIndexes()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public void StLocConstant()
    {
        int a;
        UseValue(out a);
        a = 1;
    }

    private static void UseValue(out int value)
    {
        value = 1;
    }
}");

                this.visitor.Received().StoreValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Stloc_0),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleStoreLocalsWithShortIndexes()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    public void StLocShort()
    {
        int a, b, c, d, e;
        UseValue(out a);
        UseValue(out b);
        UseValue(out c);
        UseValue(out d);
        UseValue(out e);
        e = 1;
    }

    private static void UseValue(out int value)
    {
        value = 1;
    }
}");

                this.visitor.Received().StoreValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Stloc_S),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            [Fact]
            public void ShouldHandleStoreStaticFields()
            {
                this.Visit(this.visitor, @"public class TestClass
{
    private static int staticField;
    public static void StFldStatic()
    {
        staticField += 1;
    }
}");

                this.visitor.Received().StoreValue(
                    Arg.Is<Instruction>(i => i.OpCode.Code == Code.Stsfld),
                    Arg.Is<TypeReference>(t => t.Name == nameof(Int32)));
            }

            internal class FakeCilVisitor : CilVisitor
            {
                public virtual void StoreValue(Instruction instruction, TypeReference type)
                {
                }

                protected override void OnStoreValue(Instruction instruction, TypeReference type)
                {
                    this.StoreValue(instruction, type);
                }
            }
        }
    }
}
