namespace Transform.Managed.Tests.CodeGeneration
{
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Autocrat.Transform.Managed.CodeGeneration;
    using FluentAssertions;
    using Xunit;

    public class NativeImportGeneratorTests
    {
        private readonly NativeImportGenerator generator = new NativeImportGenerator();

        private string WriteToOutput()
        {
            using var stream = new MemoryStream();
            this.generator.WriteTo(stream);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public sealed class RegisterMethodTests : NativeImportGeneratorTests
        {
            [Fact]
            public void ShouldReturnUniqueValuesForNewMethods()
            {
                int result1 = this.generator.RegisterMethod("", "Method1");
                int result2 = this.generator.RegisterMethod("", "Method2");

                result1.Should().NotBe(result2);
            }
        }

        public sealed class WriteToTests : NativeImportGeneratorTests
        {
            [Fact]
            public void ShouldWriteEachMethod()
            {
                this.generator.RegisterMethod("int {0}()", "Method1");
                this.generator.RegisterMethod("int {0}()", "Method2");

                string result = this.WriteToOutput();

                ShouldMatchOnce(result, "Method1()");
                ShouldMatchOnce(result, "Method2()");
            }

            [Fact]
            public void ShouldWriteTheAddressOfMethods()
            {
                this.generator.RegisterMethod("int {0}()", "Method");

                string result = this.WriteToOutput();

                ShouldMatchOnce(result, "&Method");
            }

            private static void ShouldMatchOnce(string input, string toMatch)
            {
                Regex.Matches(input, Regex.Escape(toMatch))
                    .Should().ContainSingle();
            }
        }
    }
}
