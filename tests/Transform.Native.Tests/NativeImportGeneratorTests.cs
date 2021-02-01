namespace Transform.Native.Tests
{
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Autocrat.Transform.Native;
    using FluentAssertions;
    using Xunit;

    public class NativeImportGeneratorTests
    {
        private readonly NativeImportGenerator generator = new NativeImportGenerator();

        private void LoadExports(params (string name, string signature)[] methods)
        {
            var buffer = new StringBuilder();
            buffer.AppendLine("[");

            bool addComma = false;
            foreach ((string name, string signature) in methods)
            {
                if (addComma)
                {
                    buffer.Append(",");
                }
                else
                {
                    addComma = true;
                }

                buffer.Append("{\"name\":\"")
                      .Append(name)
                      .Append("\",\"signature\":\"")
                      .Append(signature)
                      .AppendLine("\"}");
            }

            buffer.AppendLine("]");

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(buffer.ToString()));
            this.generator.LoadExports(ms);
        }

        private string WriteToOutput()
        {
            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                this.generator.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public sealed class WriteToTests : NativeImportGeneratorTests
        {
            [Fact]
            public void ShouldWriteEachMethod()
            {
                this.LoadExports(("Method1", "void {0}()"), ("Method2", "void {0}()"));

                string result = this.WriteToOutput();

                ShouldMatchOnce(result, "void Method1()");
                ShouldMatchOnce(result, "void Method2()");
            }

            [Fact]
            public void ShouldWriteTheAddressOfMethods()
            {
                this.LoadExports(("Method", "int {0}()"));

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
