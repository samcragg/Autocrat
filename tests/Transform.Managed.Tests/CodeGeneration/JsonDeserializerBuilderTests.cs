namespace Transform.Managed.Tests.CodeGeneration
{
    using System;
    using System.Text;
    using System.Text.Json;
    using Autocrat.Transform.Managed.CodeGeneration;
    using FluentAssertions;
    using Mono.Cecil;
    using NSubstitute;
    using Xunit;
    using SR = System.Reflection;

    public class JsonDeserializerBuilderTests
    {
        private const string ClassTypeName = "SimpleClass";
        private readonly ConfigGenerator generator = Substitute.For<ConfigGenerator>();

        private delegate Utf8JsonReader CreateReader();

        private delegate T ReadDelegate<T>(ref Utf8JsonReader reader);

        private static T ReadJson<T>(Type serializerType, CreateReader createReader)
        {
            var readMethod = (ReadDelegate<T>)Delegate.CreateDelegate(
                typeof(ReadDelegate<T>),
                Activator.CreateInstance(serializerType),
                JsonDeserializerBuilder.ReadMethodName);

            Utf8JsonReader reader = createReader();
            return readMethod(ref reader);
        }

        private object DeserializeJson(string propertyDeclarations, string json)
        {
            (Type classType, Type serializer) = this.GenerateClasses(propertyDeclarations);
            CreateReader createReader = () => new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            return typeof(JsonDeserializerBuilderTests)
                .GetMethod(nameof(ReadJson), SR.BindingFlags.NonPublic | SR.BindingFlags.Static)
                .MakeGenericMethod(classType)
                .Invoke(null, new object[] { serializer, createReader });
        }

        private (Type classType, Type serialzier) GenerateClasses(string propertyDeclarations)
        {
            string classDeclaration = @$"
#nullable enable
#pragma warning disable 8618 // Non-nullable property '...' is uninitialized.
public class {ClassTypeName}
{{
    {propertyDeclarations}
}}";
            ModuleDefinition module = CodeHelper.CompileCode(classDeclaration);
            TypeDefinition classType = module.GetType(ClassTypeName);
            var builder = new JsonDeserializerBuilder(this.generator, classType);

            foreach (PropertyDefinition property in classType.Properties)
            {
                builder.AddProperty(property);
            }

            builder.GenerateClass(module);
            SR.Assembly assembly = CodeHelper.LoadModule(module);
            return (
                assembly.GetType(ClassTypeName),
                assembly.GetType(ClassTypeName + JsonDeserializerBuilder.GeneratedClassSuffix));
        }

        public sealed class GenerateTests : JsonDeserializerBuilderTests
        {
            [Fact]
            public void ShouldAllowReadingFromTheStartObjectToken()
            {
                (Type classType, Type serializer) = this.GenerateClasses("public int Value { get; set; }");
                CreateReader createReader = () =>
                {
                    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(@"{""value"": 123}"));
                    reader.Read();
                    reader.TokenType.Should().Be(JsonTokenType.StartObject);
                    return reader;
                };

                dynamic instance = typeof(JsonDeserializerBuilderTests)
                    .GetMethod(nameof(ReadJson), SR.BindingFlags.NonPublic | SR.BindingFlags.Static)
                    .MakeGenericMethod(classType)
                    .Invoke(null, new object[] { serializer, createReader });

                ((int)instance.Value).Should().Be(123);
            }

            [Fact]
            public void ShouldIgnoreLeadingWhitespace()
            {
                dynamic instance = this.DeserializeJson(
                    "public int Value { get; set; }",
                    "\t { \"value\":123 }");

                ((int)instance.Value).Should().Be(123);
            }

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
            public void ShouldReadFromOtherSerializers()
            {
                // Note we can't use a property in the other type, as that will
                // get picked up in our test method and added to the deserializer
                // The OtherTypeDeserializer matches the overall structure that
                // are generators use to read the data
                string classMembers = @"
    public OtherType Other { get; set; }
}
public class OtherType
{
    public string field;
}
public class OtherTypeDeserializer
{
    public OtherType Read(ref System.Text.Json.Utf8JsonReader reader)
    {
        // Read start object;
        if (reader.TokenType != System.Text.Json.JsonTokenType.StartObject)
        {
            reader.Read();
        }
        var instance = new OtherType();

        // Read property
        reader.Read();

        // Read value
        reader.Read();
        instance.field = reader.GetString();

        // Read end object
        reader.Read();
        return instance;
    }";
                this.generator.GetClassFor(null)
                    .ReturnsForAnyArgs(ci => ci.Arg<TypeReference>().Module.GetType("OtherTypeDeserializer"));

                dynamic instance = this.DeserializeJson(
                    classMembers,
                    @"{ ""other"": { ""field"":""Test Value"" } }");

                ((string)instance.Other.field).Should().Be("Test Value");
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
