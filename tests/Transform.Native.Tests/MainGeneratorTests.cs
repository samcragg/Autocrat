namespace Transform.Native.Tests
{
    using System.IO;
    using System.Text;
    using Autocrat.Transform.Native;
    using FluentAssertions;
    using Xunit;

    public class MainGeneratorTests
    {
        private readonly MainGenerator generator = new MainGenerator();

        private string WriteToOutput()
        {
            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                this.generator.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public sealed class WriteToTests : MainGeneratorTests
        {
            [Fact]
            public void ShouldReturnAutocratMain()
            {
                string output = this.WriteToOutput();

                output.Should().Contain("return autocrat_main(");
            }

            [Fact]
            public void ShouldSetTheDescription()
            {
                this.generator.Description = "test description";

                string output = this.WriteToOutput();

                output.Should().Contain("set_description(\"test description\")");
            }

            [Fact]
            public void ShouldSetTheVersion()
            {
                this.generator.Version = "1.2.3.4";

                string output = this.WriteToOutput();

                output.Should().Contain("set_version(\"1.2.3.4\")");
            }
        }
    }
}
