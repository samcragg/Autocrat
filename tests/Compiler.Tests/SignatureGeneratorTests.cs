namespace Compiler.Tests
{
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Xunit;

    public class SignatureGeneratorTests
    {
        private readonly SignatureGenerator generator = new SignatureGenerator();

        public sealed class GetSignatureTests : SignatureGeneratorTests
        {
            [Fact]
            public void ShouldIncludeTheArguments()
            {
                IMethodSymbol method = CompilationHelper.CreateMethodSymbol("X", "void", "int a, float b");

                string result = this.generator.GetSignature(method);

                result.Should().Be("void {0}(std::int32_t, float)");
            }

            [Fact]
            public void ShouldIncludeTheReturnType()
            {
                IMethodSymbol method = CompilationHelper.CreateMethodSymbol("X", "int", "");

                string result = this.generator.GetSignature(method);

                result.Should().Be("std::int32_t {0}()");
            }
        }
    }
}
