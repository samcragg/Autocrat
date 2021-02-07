namespace Transform.Managed.Tests.CodeGeneration
{
    using System.Linq;
    using Autocrat.Transform.Managed;
    using Autocrat.Transform.Managed.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using NSubstitute;
    using Xunit;
    using SR = System.Reflection;

    public class ManagedCallbackGeneratorTests
    {
        private readonly ManagedCallbackGenerator generator;
        private readonly InstanceBuilder instanceBuilder;
        private readonly ExportedMethods exportedMethods;

        private ManagedCallbackGeneratorTests()
        {
            this.instanceBuilder = Substitute.For<InstanceBuilder>();
            this.exportedMethods = Substitute.For<ExportedMethods>();

            this.generator = new ManagedCallbackGenerator(
                this.instanceBuilder,
                this.exportedMethods);
        }

        public sealed class AddMethodTests : ManagedCallbackGeneratorTests
        {
            [Fact]
            public void ShouldReturnTheRegistrationOfTheGeneratedMethod()
            {
                var method = new CodeHelper.GeneratedMethod();
                this.exportedMethods.RegisterMethod("signature", Arg.Any<string>())
                    .Returns(123);

                int result = this.generator.AddMethod("signature", method.Method);

                result.Should().Be(123);
            }
        }

        public sealed class EmitTypeTests : ManagedCallbackGeneratorTests
        {
            [Fact]
            public void ShouldCallInstanceMethods()
            {
                TypeDefinition definition = CodeHelper.CompileType(@"class SimpleClass
{
    public static int InvocationCount;

    public void Method()
    {
        InvocationCount++;
    }
}");
                MethodDefinition constructor = definition.Methods.Single(m => m.Name == ".ctor");
                this.instanceBuilder
                    .WhenForAnyArgs(x => x.EmitNewObj(null, null))
                    .Do(ci =>
                    {
                        ci.Arg<ILProcessor>().Emit(OpCodes.Newobj, constructor);
                    });

                SR.MethodInfo generatedMethod = this.EmitMethod(definition, "Method");
                generatedMethod.Invoke(null, null);

                generatedMethod.DeclaringType.Assembly
                    .GetType("SimpleClass")
                    .GetField("InvocationCount")
                    .GetValue(null)
                    .Should().Be(1);
            }

            [Fact]
            public void ShouldCallMethodsWithParameters()
            {
                TypeDefinition calculator = CodeHelper.CompileType(@"class Calculator
{
    public static int Add(int a, int b)
    {
        return a + b;
    }
}");
                SR.MethodInfo generatedMethod = this.EmitMethod(calculator, "Add");

                object result = generatedMethod.Invoke(null, new object[] { 1, 2 });
                result.Should().Be(3);
            }

            private SR.MethodInfo EmitMethod(TypeDefinition type, string method)
            {
                this.generator.AddMethod(
                    "",
                    type.Methods.Single(m => m.Name == method));
                this.generator.EmitType(type.Module);

                SR.Assembly assembly = CodeHelper.LoadModule(type.Module);
                return assembly.GetType(ManagedCallbackGenerator.GeneratedClassName)
                            .GetMethods(SR.BindingFlags.Public | SR.BindingFlags.Static)
                            .Should().ContainSingle().Subject;
            }
        }
    }
}
