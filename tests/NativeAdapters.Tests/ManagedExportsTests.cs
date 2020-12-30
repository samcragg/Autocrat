namespace NativeAdapters.Tests
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using Autocrat.NativeAdapters;
    using FluentAssertions;
    using Xunit;

    public class ManagedExportsTests
    {
        // .NET 5.0 made it an error to call the method from managed code,
        // however, as we're targeting .NET Core 3.1, we can still invoke
        // it at runtime
        private static object InvokeMethod(string name, params object[] parameters)
        {
            return typeof(ManagedExports)
                .GetMethod(name)
                .Invoke(null, parameters);
        }

        public sealed class GetByteArrayTypeTests : ManagedExportsTests
        {
            [Fact]
            public void ShouldReturnTheManagedTypeHandle()
            {
                InvokeMethod(nameof(ManagedExports.GetByteArrayType))
                   .Should().Be(typeof(byte[]).TypeHandle.Value);
            }
        }

        public sealed class InitializeManagedThreadTests : ManagedExportsTests
        {
            [Fact]
            public void ShouldSetTheSynchronizationContext()
            {
                InvokeMethod(nameof(ManagedExports.InitializeManagedThread));

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

                    InvokeMethod(
                        nameof(ManagedExports.LoadConfiguration),
                        IntPtr.Zero);

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
