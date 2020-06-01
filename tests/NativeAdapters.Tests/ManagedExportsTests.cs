namespace NativeAdapters.Tests
{
    using System.Text.Json;
    using System.Threading;
    using Autocrat.NativeAdapters;
    using FluentAssertions;
    using Xunit;

    public class ManagedExportsTests
    {
        public sealed class GetByteArrayTypeTests : ManagedExportsTests
        {
            [Fact]
            public void ShouldReturnTheManagedTypeHandle()
            {
                ManagedExports.GetByteArrayType()
                    .Should().Be(typeof(byte[]).TypeHandle.Value);
            }
        }

        public sealed class InitializeManagedThreadTests : ManagedExportsTests
        {
            [Fact]
            public void ShouldSetTheSynchronizationContext()
            {
                ManagedExports.InitializeManagedThread();

                SynchronizationContext.Current
                    .Should().BeOfType<TaskServiceSynchronizationContext>();
            }
        }

        [Collection(nameof(ConfigService))]
        public sealed class LoadConfigurationTests : ManagedExportsTests
        {
            [Fact]
            public void ShouldCallTheConfigService()
            {
                try
                {
                    bool loadCalled = false;
                    ConfigService.Initialize((ref Utf8JsonReader reader) => loadCalled = true);

                    ManagedExports.LoadConfiguration(new byte[0]);

                    loadCalled.Should().BeTrue();
                }
                finally
                {
                    ConfigService.Initialize(null);
                }
            }
        }
    }
}
