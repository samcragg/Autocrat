namespace Transform.Native.Tests
{
    using System;
    using System.IO;
    using Autocrat.Common;
    using Autocrat.Transform.Native;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class NativeCodeGeneratorTests
    {
        private readonly NativeCodeGenerator generator;
        private readonly NativeImportGenerator importGenerator;
        private readonly MainGenerator main;
        private readonly IOutputFile output;

        private NativeCodeGeneratorTests()
        {
            this.importGenerator = Substitute.For<NativeImportGenerator>();
            this.main = Substitute.For<MainGenerator>();
            this.output = Substitute.For<IOutputFile>();
            this.output.Stream.Returns(Stream.Null);

            this.generator = new NativeCodeGenerator(this.main, this.importGenerator);
        }

        public sealed class GenerateTests : NativeCodeGeneratorTests
        {
            [Fact]
            public void ShouldValidateArguments()
            {
                this.generator.Invoking(x => x.Generate(null, this.output))
                    .Should().Throw<ArgumentNullException>();

                this.generator.Invoking(x => x.Generate(Stream.Null, null))
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldWritetheMainMethod()
            {
                this.generator.Generate(Stream.Null, this.output);

                this.main.ReceivedWithAnyArgs().WriteTo(null);
            }

            [Fact]
            public void ShouldWriteTheNativeImports()
            {
                Stream json = Substitute.For<Stream>();
                this.generator.Generate(json, this.output);

                this.importGenerator.Received().LoadExports(json);
                this.importGenerator.ReceivedWithAnyArgs().WriteTo(null);
            }
        }
    }
}
