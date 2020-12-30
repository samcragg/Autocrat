namespace Compiler.Tests.CodeGeneration
{
    using System.Linq;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using Xunit;

    public class NullableTypeReferenceTests
    {
        public sealed class CreateTests : NullableTypeReferenceTests
        {
            [Theory]
            [InlineData("int?[]")]
            [InlineData("int?[]?")]
            [InlineData("int?[][]")]
            [InlineData("string?[]")]
            [InlineData("string?[]?")]
            [InlineData("string?[][]")]
            public void ShouldHandleNullableArrays(string type)
            {
                NullableTypeReference result = CreateForType(type);
                while (result.Element != null)
                {
                    result = result.Element;
                }

                result.AllowsNulls.Should().BeTrue();
            }

            [Theory]
            [InlineData("int?")]
            [InlineData("int[]?")]
            [InlineData("string?")]
            [InlineData("string[]?")]
            public void ShouldHandleNullableTypes(string type)
            {
                NullableTypeReference result = CreateForType(type);
                result.AllowsNulls.Should().BeTrue();
            }

            private static NullableTypeReference CreateForType(string type)
            {
                string code = @$"
#nullable enable

public class TestClass
{{
    public {type} Property {{ get; set; }} = null!;
}}";
                ModuleDefinition module = CodeHelper.CompileCode(code);
                PropertyDefinition property = module.GetType("TestClass").Properties.Single();

                return NullableTypeReference.Create(property);
            }
        }
    }
}
