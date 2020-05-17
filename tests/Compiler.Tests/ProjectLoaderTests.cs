namespace Compiler.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Xunit;

    [Trait("Category", "Integration")]
    public class ProjectLoaderTests
    {
        private readonly ProjectLoader loader = new ProjectLoader();

        public sealed class GetCompilationsAsyncTests : ProjectLoaderTests
        {
            [Fact]
            public async Task ShouldLoadAllTheSources()
            {
                using var class1 = new TemporaryFile();
                WriteClassToFile(class1.Filename, "Class1");

                using var class2 = new TemporaryFile();
                WriteClassToFile(class2.Filename, "Class2");

                Compilation compilation = await this.loader.GetCompilationAsync(
                    new[]
                    {
                        typeof(object).Assembly.Location,
                    },
                    new[]
                    {
                        class1.Filename,
                        class2.Filename,
                    });

                compilation.GetDiagnostics().Should().BeEmpty();
                compilation.SyntaxTrees.Should().HaveCount(2);
            }

            private static void WriteClassToFile(string path, string name)
            {
                File.WriteAllText(path, "class " + name + " { }");
            }
        }

        private sealed class TemporaryFile : IDisposable
        {
            public TemporaryFile()
            {
                this.Filename = Path.GetTempFileName();
            }

            public string Filename { get; }

            public void Dispose()
            {
                try
                {
                    File.Delete(this.Filename);
                }
                catch
                {
                }
            }
        }
    }
}
