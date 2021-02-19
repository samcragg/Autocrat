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
        private readonly StaticInitializerGenerator staticInitializer;

        private NativeCodeGeneratorTests()
        {
            this.importGenerator = Substitute.For<NativeImportGenerator>();
            this.main = Substitute.For<MainGenerator>();
            this.staticInitializer = Substitute.For<StaticInitializerGenerator>();
            this.output = Substitute.For<IOutputFile>();
            this.output.Stream.Returns(Stream.Null);

            this.generator = new NativeCodeGenerator(
                this.main,
                this.importGenerator,
                this.staticInitializer);
        }

        public sealed class GenerateTests : NativeCodeGeneratorTests
        {
            [Fact]
            public void ShouldValidateArguments()
            {
                this.generator.Invoking(x => x.Generate(null, Stream.Null, this.output))
                    .Should().Throw<ArgumentNullException>();

                this.generator.Invoking(x => x.Generate(Stream.Null, null, this.output))
                    .Should().Throw<ArgumentNullException>();

                this.generator.Invoking(x => x.Generate(Stream.Null, Stream.Null, null))
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldWritetheMainMethod()
            {
                this.generator.Generate(Stream.Null, Stream.Null, this.output);

                this.main.ReceivedWithAnyArgs().WriteTo(null);
            }

            [Fact]
            public void ShouldWriteTheNativeImports()
            {
                Stream json = Substitute.For<Stream>();
                this.generator.Generate(json, Stream.Null, this.output);

                this.importGenerator.Received().LoadExports(json);
                this.importGenerator.ReceivedWithAnyArgs().WriteTo(null);
            }

            [Fact]
            public void ShouldWriteTheStaticInitialization()
            {
                Stream map = Substitute.For<Stream>();
                this.generator.Generate(Stream.Null, map, this.output);

                this.staticInitializer.Received().Load(map);
                this.staticInitializer.ReceivedWithAnyArgs().WriteTo(null);
            }
        }
    }
}
