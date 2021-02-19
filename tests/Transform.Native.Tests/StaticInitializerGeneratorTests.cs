namespace Transform.Native.Tests
{
    using System.IO;
    using System.Text;
    using Autocrat.Transform.Native;
    using FluentAssertions;
    using Xunit;

    public class StaticInitializerGeneratorTests
    {
        private readonly StaticInitializerGenerator generator = new StaticInitializerGenerator();

        private string WriteToOutput()
        {
            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                this.generator.WriteTo(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public sealed class WriteToTests : StaticInitializerGeneratorTests
        {
            [Fact]
            public void ShouldCallEachStaticStub()
            {
                const string Xml = @"<?xml version='1.0' encoding='utf-8'?>
<ObjectNodes>
  <ReadyToRunHelper Name='__VirtualCall_Namespace_Class' Length='7'  />
  <ReadyToRunHelper Name='__GetGCStaticBase_Namespace_Class' Length='35' />
  <ReadyToRunHelper Name='__GetNonGCStaticBase_Namespace_Class' Length='29' />
</ObjectNodes>";
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(Xml));
                this.generator.Load(ms);

                string result = this.WriteToOutput();

                result.Should().Contain("__GetGCStaticBase_Namespace_Class");
                result.Should().Contain("__GetNonGCStaticBase_Namespace_Class");
                result.Should().NotContain("__VirtualCall_Namespace_Class");
            }

            [Fact]
            public void ShouldWriteTheInitializeMethod()
            {
                string result = this.WriteToOutput();

                result.Should().Contain("void initialize_statics()");
            }
        }
    }
}
