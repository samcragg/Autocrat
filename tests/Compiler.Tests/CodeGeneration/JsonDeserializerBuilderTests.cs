namespace Compiler.Tests.CodeGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Autocrat.Compiler.CodeGeneration;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Xunit;
    using static System.Reflection.BindingFlags;

    public class JsonDeserializerBuilderTests
    {
        private const string ClassTypeName = "SimpleClass";

        private delegate T ReadDelegate<T>(ref Utf8JsonReader reader);

        private static T ReadJson<T>(Type serializerType, string json)
        {
            var readMethod = (ReadDelegate<T>)Delegate.CreateDelegate(
                typeof(ReadDelegate<T>),
                Activator.CreateInstance(serializerType),
                JsonDeserializerBuilder.ReadMethodName);

            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            return readMethod(ref reader);
        }

        private object DeserializeJson(string propertyDeclarations, string json)
        {
            string classDeclaration = @$"
#nullable enable
#pragma warning disable 8618 // Non-nullable property '...' is uninitialized.
public class {ClassTypeName}
{{
    {propertyDeclarations}
}}";
            Compilation compilation = CompilationHelper.CompileCode(classDeclaration);
            SyntaxTree tree = compilation.SyntaxTrees.Single();
            SemanticModel model = compilation.GetSemanticModel(tree);
            var generator = new JsonDeserializerBuilder(
                SyntaxFactory.IdentifierName(ClassTypeName));

            IEnumerable<PropertyDeclarationSyntax> properties =
                tree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>();

            foreach (PropertyDeclarationSyntax property in properties)
            {
                generator.AddProperty(model.GetDeclaredSymbol(property));
            }

            string generatedCode = SyntaxFactory
                .CompilationUnit()
                .WithUsings(SyntaxFactory.List(JsonDeserializerBuilder.GetUsingStatements()))
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(generator.GenerateClass()))
                .NormalizeWhitespace()
                .ToFullString();

            Compilation generatedCompilation = CompilationHelper.CompileCode(
                generatedCode + classDeclaration,
                referenceAbstractions: true);

            System.Reflection.Assembly assembly = CompilationHelper.GenerateAssembly(generatedCompilation);
            Type classType = assembly.GetType(ClassTypeName);
            Type serializerType = assembly.GetType(ClassTypeName + JsonDeserializerBuilder.GeneratedClassSuffix);

            return typeof(JsonDeserializerBuilderTests)
                .GetMethod(nameof(ReadJson), NonPublic | Static)
                .MakeGenericMethod(classType)
                .Invoke(null, new object[] { serializerType, json });
        }

        public sealed class GenerateTests : JsonDeserializerBuilderTests
        {
            [Fact]
            public void ShouldIgnoreUnknownJsonProperties()
            {
                dynamic instance = this.DeserializeJson(
                    "public int Value { get; set; }",
                    @"{ ""unknown"":{}, ""value"":123 }");

                ((int)instance.Value).Should().Be(123);
            }

            [Fact]
            public void ShouldReadArraysOfNullableReferences()
            {
                dynamic instance = this.DeserializeJson(
                    "public string?[] Array { get; set; }",
                    @"{ ""array"":[""one"",null,""two""] }");

                string[] array = instance.Array;
                array.Should().Equal("one", null, "two");
            }

            [Fact]
            public void ShouldReadArraysOfNullableValues()
            {
                dynamic instance = this.DeserializeJson(
                    "public int?[] Array { get; set; }",
                    @"{ ""array"":[1,null,2] }");

                int?[] array = instance.Array;
                array.Should().Equal(1, null, 2);
            }

            [Fact]
            public void ShouldReadArraysOfPrimitives()
            {
                dynamic instance = this.DeserializeJson(
                    "public int[] Array { get; set; }",
                    @"{ ""array"":[1,2,3] }");

                int[] array = instance.Array;
                array.Should().Equal(1, 2, 3);
            }

            [Fact]
            public void ShouldReadByteArrays()
            {
                string encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test"));
                string json = @"{ ""data"":""" + encodedData + @""" }";

                dynamic instance = this.DeserializeJson("public byte[] Data { get; set; }", json);

                byte[] data = instance.Data;
                Encoding.UTF8.GetString(data).Should().Be("Test");
            }

            [Theory]
            [InlineData("bool", "true")]
            [InlineData("byte", "123")]
            [InlineData("decimal", "1.5")]
            [InlineData("double", "1.5")]
            [InlineData("float", "1.5")]
            [InlineData("int", "123")]
            [InlineData("long", "123")]
            [InlineData("sbyte", "123")]
            [InlineData("short", "123")]
            [InlineData("string", "'text'")]
            [InlineData("uint", "123")]
            [InlineData("ushort", "123")]
            [InlineData("ulong", "123")]
            [InlineData("System.Guid", "'12345678-1234-1234-1234-1234567890AB'")]
            public void ShouldReadCommonTypes(string propertyType, string value)
            {
                string property = "public " + propertyType + " Property { get; set; }";
                string json = "{\"property\":" + value.Replace('\'', '"') + "}";

                dynamic instance = this.DeserializeJson(property, json);
                string propertyValue = instance.Property.ToString();

                propertyValue.Should().BeEquivalentTo(value.Replace("'", null));
            }

            [Fact]
            public void ShouldReadDateTimeTypes()
            {
                string properties = @"
    public System.DateTime DateTime { get; set; }
    public System.DateTimeOffset DateTimeOffset { get; set; }";

                string json = @"{
""dateTime"":""2011-12-13T14:15:16Z"",
""dateTimeOffset"":""2011-12-13T14:15:16-01:00""
}";

                dynamic instance = this.DeserializeJson(properties, json);

                DateTime dateTime = instance.DateTime;
                dateTime.Should().Be(new DateTime(2011, 12, 13, 14, 15, 16));

                DateTimeOffset dateTimeOffset = instance.DateTimeOffset;
                dateTimeOffset.Offset.Should().Be(new TimeSpan(-1, 0, 0));
                dateTimeOffset.DateTime.Should().Be(new DateTime(2011, 12, 13, 14, 15, 16));
            }

            [Fact]
            public void ShouldReadEnumValuesFromNumbers()
            {
                string classMembers = @"
    public enum MyEnum { EnumValue = 2 }
    public MyEnum EnumProperty { get; set; }";

                dynamic instance = this.DeserializeJson(
                    classMembers,
                    @"{ ""enumProperty"":2 }");

                ((string)instance.EnumProperty.ToString()).Should().Be("EnumValue");
            }

            [Fact]
            public void ShouldReadEnumValuesFromText()
            {
                string classMembers = @"
    public enum MyEnum { EnumValue }
    public MyEnum EnumProperty { get; set; }";

                dynamic instance = this.DeserializeJson(
                    classMembers,
                    @"{ ""enumProperty"":""enumValue"" }");

                ((string)instance.EnumProperty.ToString()).Should().Be("EnumValue");
            }

            [Fact]
            public void ShouldReadNullableReferences()
            {
                dynamic instance = this.DeserializeJson(
                    @"public string? Value { get; set; } = ""text"";",
                    @"{""value"":null}");

                string value = instance.Value;
                value.Should().BeNull();
            }

            [Fact]
            public void ShouldReadNullableValues()
            {
                dynamic instance = this.DeserializeJson(
                    @"public int? Value { get; set; } = 123;",
                    @"{""value"":null}");

                int? value = instance.Value;
                value.Should().BeNull();
            }
        }
    }
}
