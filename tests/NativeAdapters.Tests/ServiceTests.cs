namespace NativeAdapters.Tests
{
    using System;
    using System.Reflection;
    using Autocrat.Abstractions;
    using Autocrat.NativeAdapters;
    using FluentAssertions;
    using Xunit;

    public class ServiceTests
    {
        private static void AssertImplementsInterface<TInterface>(Type classType)
        {
            static Type GetParameterType(ParameterInfo pi)
            {
                if (pi.ParameterType.GetCustomAttribute<NativeDelegateAttribute>() != null)
                {
                    return typeof(int);
                }
                else
                {
                    return pi.ParameterType;
                }
            }

            foreach (MethodInfo method in typeof(TInterface).GetMethods())
            {
                Type[] parameters = Array.ConvertAll(method.GetParameters(), GetParameterType);

                MethodInfo classMethod = classType.GetMethod(method.Name, parameters);
                
                classMethod.Should().NotBeNull();
                classMethod.IsStatic.Should().BeTrue();
                classMethod.ReturnType.FullName.Should().Be(method.ReturnType.FullName);
            }
        }

        public sealed class NetworkServiceTests : ServiceTests
        {
            [Fact]
            public void ShouldImplementTheInterfaceMethods()
            {
                AssertImplementsInterface<INetworkService>(typeof(NetworkService));
            }
        }

        public sealed class TimerServiceTests : ServiceTests
        {
            [Fact]
            public void ShouldImplementTheInterfaceMethods()
            {
                AssertImplementsInterface<ITimerService>(typeof(TimerService));
            }
        }

        public sealed class WorkerFactoryTests : ServiceTests
        {
            [Fact]
            public void ShouldImplementTheInterfaceMethods()
            {
                AssertImplementsInterface<IWorkerFactory>(typeof(WorkerFactory));
            }
        }
    }
}
