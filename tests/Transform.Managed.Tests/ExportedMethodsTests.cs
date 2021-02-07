namespace Transform.Managed.Tests
{
    using System.IO;
    using System.Text.Json;
    using Autocrat.Transform.Managed;
    using FluentAssertions;
    using Xunit;

    public class ExportedMethodsTests
    {
        private readonly ExportedMethods exportedMethods = new ExportedMethods();

        public sealed class RegisterMethodTests : ExportedMethodsTests
        {
            [Fact]
            public void ShouldReturnTheIndexOfTheMethod()
            {
                int first = this.exportedMethods.RegisterMethod("", "");
                int second = this.exportedMethods.RegisterMethod("", "");

                first.Should().Be(0);
                second.Should().Be(1);
            }
        }

        public sealed class SerializeToTests : ExportedMethodsTests
        {
            [Fact]
            public void ShouldSaveJsonOutput()
            {
                // As we're exporting this between projects that don't depend
                // on each other, it's important that the format is known so
                // that the deserializer works correctly.
                using var ms = new MemoryStream();
                this.exportedMethods.RegisterMethod("method signature", "method name");

                this.exportedMethods.SerializeTo(ms);
                JsonElement result = JsonDocument.Parse(ms.ToArray()).RootElement;

                result.GetArrayLength().Should().Be(1);
                result[0].GetProperty("name").GetString().Should().Be("method name");
                result[0].GetProperty("signature").GetString().Should().Be("method signature");
            }
        }
    }
}
