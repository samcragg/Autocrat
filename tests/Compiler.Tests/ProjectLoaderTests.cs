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
            public async Task ShouldLoadAllTheProjects()
            {
                using (var project1 = new TemporaryFile())
                using (var project2 = new TemporaryFile())
                {
                    WriteProjectFile(project1.Filename);
                    WriteProjectFile(project2.Filename);

                    Compilation[] compilations = await this.loader.GetCompilationsAsync(new[]
                    {
                        project1.Filename,
                        project2.Filename
                    });

                    compilations.Should().HaveCount(2);
                }
            }

            private static void WriteProjectFile(string path)
            {
                const string Template = @"<Project Sdk='Microsoft.NET.Sdk'>
<PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
</Project>";

                File.WriteAllText(path, Template);
            }
        }

        private sealed class TemporaryFile : IDisposable
        {
            public TemporaryFile()
            {
                // The file MUST end with "csproj" so the loader know what language
                // the project is in
                this.Filename = Path.Combine(
                    Path.GetTempPath(),
                    Guid.NewGuid() + ".csproj");
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
