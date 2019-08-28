namespace Compiler.Tests
{
    using Autocrat.Compiler;
    using FluentAssertions;
    using Xunit;

    public class ServiceFactoryTests
    {
        private readonly ServiceFactory factory;

        private ServiceFactoryTests()
        {
            this.factory = new ServiceFactory(CompilationHelper.CompileCode(""));
        }

        public sealed class CreateInitializerGeneratorTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                InitializerGenerator result1 = this.factory.CreateInitializerGenerator();
                InitializerGenerator result2 = this.factory.CreateInitializerGenerator();

                result1.Should().NotBeSameAs(result2);
            }
        }

        public sealed class CreateInstanceBuilderTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                InstanceBuilder result1 = this.factory.CreateInstanceBuilder();
                InstanceBuilder result2 = this.factory.CreateInstanceBuilder();

                result1.Should().NotBeSameAs(result2);
            }
        }

        public sealed class CreateNativeRegisterRewriterTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                NativeRegisterRewriter result1 = this.factory.CreateNativeRegisterRewriter(null);
                NativeRegisterRewriter result2 = this.factory.CreateNativeRegisterRewriter(null);

                result1.Should().NotBeSameAs(result2);
            }
        }

        public sealed class GetConstructorResolverTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                ConstructorResolver result1 = this.factory.GetConstructorResolver();
                ConstructorResolver result2 = this.factory.GetConstructorResolver();

                result1.Should().BeSameAs(result2);
            }
        }

        public sealed class GetInterfaceResolverTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                InterfaceResolver result1 = this.factory.GetInterfaceResolver();
                InterfaceResolver result2 = this.factory.GetInterfaceResolver();

                result1.Should().BeSameAs(result2);
            }
        }

        public sealed class GetManagedCallbackGeneratorTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                ManagedCallbackGenerator result1 = this.factory.GetManagedCallbackGenerator();
                ManagedCallbackGenerator result2 = this.factory.GetManagedCallbackGenerator();

                result1.Should().BeSameAs(result2);
            }
        }

        public sealed class GetNativeImportGeneratorTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                NativeImportGenerator result1 = this.factory.GetNativeImportGenerator();
                NativeImportGenerator result2 = this.factory.GetNativeImportGenerator();

                result1.Should().BeSameAs(result2);
            }
        }
    }
}
