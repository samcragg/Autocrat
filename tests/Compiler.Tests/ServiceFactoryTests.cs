namespace Compiler.Tests
{
    using Autocrat.Compiler;
    using Autocrat.Compiler.CodeGeneration;
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

        public sealed class CreateSyntaxTreeRewriterTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                SyntaxTreeRewriter result1 = this.factory.CreateSyntaxTreeRewriter();
                SyntaxTreeRewriter result2 = this.factory.CreateSyntaxTreeRewriter();

                result1.Should().NotBeSameAs(result2);
            }
        }

        public sealed class GetConfigGeneratorTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                ConfigGenerator result1 = this.factory.GetConfigGenerator();
                ConfigGenerator result2 = this.factory.GetConfigGenerator();

                result1.Should().BeSameAs(result2);
            }
        }

        public sealed class GetConfigResolverTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                ConfigResolver result1 = this.factory.GetConfigResolver();
                ConfigResolver result2 = this.factory.GetConfigResolver();

                result1.Should().BeSameAs(result2);
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
