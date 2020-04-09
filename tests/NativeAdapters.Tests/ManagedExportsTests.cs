namespace NativeAdapters.Tests
{
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
    }
}
