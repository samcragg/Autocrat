namespace Compiler.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Xunit;
    using static System.Reflection.BindingFlags;

    public class DeserializerGeneratorTests
    {
        private const string ClassTypeName = "SimpleClass";

        private readonly DeserializerGenerator generator = new DeserializerGenerator(
            SyntaxFactory.IdentifierName(ClassTypeName));

        private delegate T ReadDelegate<T>(ref Utf8JsonReader reader);

        private static T ReadJson<T>(Type serializerType, string json)
        {
            var readMethod = (ReadDelegate<T>)Delegate.CreateDelegate(
                typeof(ReadDelegate<T>),
                Activator.CreateInstance(serializerType),
                DeserializerGenerator.ReadMethodName);

            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            return readMethod(ref reader);
        }

        private object DeserializeJson(string propertyDeclarations, string json, bool allowNulls = false)
        {
            string classDeclaration = "public class " + ClassTypeName + " {" + propertyDeclarations + "}";
            Compilation compilation = CompilationHelper.CompileCode(classDeclaration);

            SyntaxTree tree = compilation.SyntaxTrees.Single();
            IEnumerable<PropertyDeclarationSyntax> properties =
                tree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>();

            foreach (PropertyDeclarationSyntax property in properties)
            {
                TypeInfo typeInfo = compilation
                    .GetSemanticModel(tree)
                    .GetTypeInfo(property.Type);

                this.generator.AddProperty(property.Identifier.ValueText, typeInfo.Type, allowNulls);
            }

            string generatedCode = this.generator.Generate()
                .NormalizeWhitespace()
                .ToFullString();

            Compilation generatedCompilation = CompilationHelper.CompileCode(
                generatedCode + classDeclaration,
                referenceAbstractions: true);

            System.Reflection.Assembly assembly = CompilationHelper.GenerateAssembly(generatedCompilation);
            Type classType = assembly.GetType(ClassTypeName);
            Type serializerType = assembly.GetType(ClassTypeName + DeserializerGenerator.GeneratedClassSuffix);

            return typeof(DeserializerGeneratorTests)
                .GetMethod(nameof(ReadJson), NonPublic | Static)
                .MakeGenericMethod(classType)
                .Invoke(null, new object[] { serializerType, json });
        }

        public sealed class GenerateTests : DeserializerGeneratorTests
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
            public void ShouldReadNullValues()
            {
                dynamic instance = this.DeserializeJson(
                    @"public string Value { get; set; } = ""DefaultValue"";",
                    @"{""value"":null}",
                    allowNulls: true);

                string value = instance.Value;
                value.Should().BeNull();
            }
        }
    }
}
