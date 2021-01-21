namespace Compiler.Tests
{
    using System;
    using System.IO;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Xunit;

    public class OutputFileTests
    {
        public sealed class StreamTests : OutputFileTests
        {
            [Fact]
            public void ShouldCreateTheDirectoryForTheFile()
            {
                string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.Exists(directory).Should().BeFalse();

                using (var file = new OutputFile(Path.Combine(directory, "test.txt")))
                {
                    file.Stream.Should().NotBeNull();
                }

                Directory.Exists(directory).Should().BeTrue();
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
