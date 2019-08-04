namespace Compiler.Tests
{
    using System;
    using System.Threading.Tasks;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using NSubstitute;
    using Xunit;

    public class ProgramTests
    {
        private readonly CodeGenerator generator = Substitute.For<CodeGenerator>();
        private readonly ProjectLoader loader = Substitute.For<ProjectLoader>();
        private readonly OutputStreams output = Substitute.For<OutputStreams>();

        public sealed class CompileCodeAsync : ProgramTests
        {
            [Fact]
            public async Task ShouldCatchAllExceptionsAndReturnAnErrorCode()
            {
                this.output.Assembly.ReturnsForAnyArgs(x => throw new DivideByZeroException());

                int result = await Program.CompileCodeAsync(null, this.output, this.loader, this.generator);

                result.Should().NotBe(0);
            }

            [Fact]
            public async Task ShouldGenerateCodeForAllTheProjects()
            {
                Compilation[] compilations =
                {
                    CompilationHelper.CompileCode("namespace A {}"),
                    CompilationHelper.CompileCode("namespace B {}"),
                };

                string[] projects = { "1", "2" };
                this.loader.GetCompilationsAsync(projects)
                    .Returns(compilations);

                await Program.CompileCodeAsync(projects, this.output, this.loader, this.generator);

                this.generator.Received().Add(compilations[0]);
                this.generator.Received().Add(compilations[1]);
            }
        }
    }
}
