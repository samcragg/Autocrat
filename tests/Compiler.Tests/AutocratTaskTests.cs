namespace Compiler.Tests
{
    using System.Threading.Tasks;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using NSubstitute;
    using Xunit;

    public class AutocratTaskTests
    {
        private readonly CodeGenerator generator = Substitute.For<CodeGenerator>();
        private readonly ProjectLoader loader = Substitute.For<ProjectLoader>();
        private readonly OutputStreams output = Substitute.For<OutputStreams>();
        private readonly AutocratTask task = new AutocratTask();

        public sealed class ExecuteAsyncTests : AutocratTaskTests
        {
            [Fact]
            public async Task ShouldGenerateCodeForAllTheSources()
            {
                Compilation compilation = CompilationHelper.CompileCode("namespace A {}");

                this.task.References = new[] { "assembly.dll" };
                this.task.Sources = new[] { "a.cs" };
                this.loader.GetCompilationAsync(this.task.References, this.task.Sources)
                    .Returns(compilation);

                Compilation captured = null;
                await this.task.ExecuteAsync(
                    this.output,
                    this.loader,
                    c =>
                    {
                        captured = c;
                        return this.generator;
                    });

                captured.Should().BeSameAs(compilation);
            }
        }
    }
}
