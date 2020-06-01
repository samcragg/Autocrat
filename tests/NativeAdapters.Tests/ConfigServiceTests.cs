namespace NativeAdapters.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using Autocrat.NativeAdapters;
    using FluentAssertions;
    using Xunit;

    [Collection(nameof(ConfigService))]
    public class ConfigServiceTests : IDisposable
    {
        public void Dispose()
        {
            ConfigService.Initialize(null);
        }

        public sealed class LoadTests : ConfigServiceTests
        {
            [Fact]
            public void ShouldAllowTrailingCommas()
            {
                byte[] source = Encoding.UTF8.GetBytes(@"[1,2,]");

                var tokens = new List<JsonTokenType>();
                ConfigService.Initialize((ref Utf8JsonReader reader) =>
                {
                    while (reader.Read())
                    {
                        tokens.Add(reader.TokenType);
                    }
                });

                ConfigService.Load(source);

                tokens.Should().Equal(
                    JsonTokenType.StartArray,
                    JsonTokenType.Number,
                    JsonTokenType.Number,
                    JsonTokenType.EndArray);
            }

            [Fact]
            public void ShouldIgnoreJsonComments()
            {
                byte[] source = Encoding.UTF8.GetBytes(@"
// Single line
/* Multi line */
""string value""");

                string jsonValue = null;
                ConfigService.Initialize((ref Utf8JsonReader reader) =>
                {
                    reader.Read().Should().BeTrue();
                    jsonValue = reader.GetString();
                });

                ConfigService.Load(source);

                jsonValue.Should().Be("string value");
            }

            [Fact]
            public void ShouldReturnFalseForExceptions()
            {
                ConfigService.Initialize((ref Utf8JsonReader reader) =>
                    throw new FormatException());

                bool result = ConfigService.Load(new byte[0]);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfThereAreNoCallbacks()
            {
                bool result = ConfigService.Load(new byte[0]);

                result.Should().BeTrue();
            }
        }
    }
}
