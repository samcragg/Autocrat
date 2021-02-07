namespace Common.Tests
{
    using Autocrat.Common;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class LogManagerTests
    {
        public sealed class GetLoggerTests : LogManagerTests
        {
            [Fact]
            public void ShouldDefaultToANonNullValue()
            {
                ILogger result = LogManager.GetLogger();

                result.Should().NotBeNull();
            }

            [Fact]
            public void ShouldReturnTheSetValue()
            {
                ILogger original = LogManager.GetLogger();
                ILogger newLogger = Substitute.For<ILogger>();

                try
                {
                    LogManager.SetLogger(newLogger);
                    ILogger result = LogManager.GetLogger();

                    result.Should().BeSameAs(newLogger);
                }
                finally
                {
                    LogManager.SetLogger(original);
                }
            }
        }
    }
}
