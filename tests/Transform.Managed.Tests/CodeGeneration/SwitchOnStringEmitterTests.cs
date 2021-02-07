namespace Transform.Managed.Tests.CodeGeneration
{
    using System;
    using System.IO;
    using Autocrat.Transform.Managed.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Xunit;
    using SR = System.Reflection;

    public class SwitchOnStringEmitterTests
    {
        private readonly SwitchOnStringEmitter emitter;
        private readonly MethodDefinition method;
        private readonly VariableDefinition resultVar;
        private readonly VariableDefinition valueVar;

        private SwitchOnStringEmitterTests()
        {
            var assembly = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("TestAssembly", new Version(0, 1)),
                "TestModule",
                ModuleKind.Console);
            ModuleDefinition module = assembly.MainModule;

            var type = new TypeDefinition(
                "TestNamespace",
                "TestClass",
                TypeAttributes.Public | TypeAttributes.BeforeFieldInit,
                module.TypeSystem.Object);
            module.Types.Add(type);

            this.method = new MethodDefinition(
                "TestMethod",
                MethodAttributes.Public | MethodAttributes.Static,
                module.TypeSystem.Int32);
            type.Methods.Add(this.method);

            this.resultVar = new VariableDefinition(module.TypeSystem.Int32);
            this.method.Body.Variables.Add(this.resultVar);

            this.valueVar = new VariableDefinition(module.TypeSystem.String);
            this.method.Body.Variables.Add(this.valueVar);

            this.emitter = new SwitchOnStringEmitter(
                module,
                this.method.Body.GetILProcessor(),
                this.valueVar);
        }

        private int EmitAndRunCode(string value)
        {
            ILProcessor il = this.method.Body.GetILProcessor();
            il.Emit(OpCodes.Ldstr, value);
            il.Emit(OpCodes.Stloc, this.valueVar);
            this.emitter.Emit();
            il.Emit(OpCodes.Ldloc, this.resultVar);
            il.Emit(OpCodes.Ret);

            using var stream = new MemoryStream();
            this.method.Module.Write(stream);

            var assembly = SR.Assembly.Load(stream.ToArray());
            SR.MethodInfo method = assembly.GetType("TestNamespace.TestClass").GetMethod("TestMethod");
            return (int)method.Invoke(null, null);
        }

        public sealed class EmitTests : SwitchOnStringEmitterTests
        {
            [Fact]
            public void ShouldAddTheDefaultCase()
            {
                this.emitter.DefaultCase = il =>
                {
                    il.Emit(OpCodes.Ldc_I4, 123);
                    il.Emit(OpCodes.Stloc, this.resultVar);
                };

                int result = this.EmitAndRunCode("unknown");

                result.Should().Be(123);
            }

            [Theory]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(3)]
            [InlineData(5)]
            [InlineData(10)]
            public void ShouldEmitTheNumberOfCases(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int value = i; // Prevent the last value of i being captured
                    this.emitter.Add(value.ToString(), il =>
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                        il.Emit(OpCodes.Stloc, this.resultVar);
                    });
                }

                for (int i = 0; i < count; i++)
                {
                    this.method.Body.Instructions.Clear();
                    int result = this.EmitAndRunCode(i.ToString());
                    result.Should().Be(i);
                }
            }
        }
    }
}
