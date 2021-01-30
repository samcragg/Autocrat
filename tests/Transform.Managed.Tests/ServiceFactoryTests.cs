namespace Transform.Managed.Tests
{
    using System;
    using Autocrat.Transform.Managed;
    using Autocrat.Transform.Managed.CodeGeneration;
    using Autocrat.Transform.Managed.CodeRewriting;
    using FluentAssertions;
    using Mono.Cecil;
    using Xunit;

    public class ServiceFactoryTests
    {
        private readonly ServiceFactory factory = new ServiceFactory();

        public sealed class CreateAssemblyLoaderTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                AssemblyLoader result1 = this.factory.CreateAssemblyLoader();
                AssemblyLoader result2 = this.factory.CreateAssemblyLoader();

                result1.Should().NotBeSameAs(result2);
            }
        }

        public sealed class CreateCodeGeneratorTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                CodeGenerator result1 = this.factory.CreateCodeGenerator(null);
                CodeGenerator result2 = this.factory.CreateCodeGenerator(null);

                result1.Should().NotBeSameAs(result2);
            }
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

        public sealed class CreateInterfaceRewriterTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                InterfaceRewriter result1 = this.factory.CreateInterfaceRewriter();
                InterfaceRewriter result2 = this.factory.CreateInterfaceRewriter();

                result1.Should().NotBeSameAs(result2);
            }
        }

        public sealed class CreateModuleRewriterTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                ModuleRewriter result1 = this.factory.CreateModuleRewriter();
                ModuleRewriter result2 = this.factory.CreateModuleRewriter();

                result1.Should().NotBeSameAs(result2);
            }
        }

        public sealed class CreateNativeDelegateRewriterTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                NativeDelegateRewriter result1 = this.factory.CreateNativeDelegateRewriter();
                NativeDelegateRewriter result2 = this.factory.CreateNativeDelegateRewriter();

                result1.Should().NotBeSameAs(result2);
            }
        }

        public sealed class CreateWorkerRegisterGeneratorTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnANewInstance()
            {
                TypeReference[] types = Array.Empty<TypeReference>();
                WorkerRegisterGenerator result1 = this.factory.CreateWorkerRegisterGenerator(types);
                WorkerRegisterGenerator result2 = this.factory.CreateWorkerRegisterGenerator(types);

                result1.Should().NotBeSameAs(result2);
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

        public sealed class GetExportedMethodsTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                ExportedMethods result1 = this.factory.GetExportedMethods();
                ExportedMethods result2 = this.factory.GetExportedMethods();

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

        public sealed class GetKnownTypeTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                KnownTypes result1 = this.factory.GetKnownTypes();
                KnownTypes result2 = this.factory.GetKnownTypes();

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

        public sealed class GetManagedExportsGeneratorTests : ServiceFactoryTests
        {
            [Fact]
            public void ShouldReturnTheSameInstance()
            {
                ManagedExportsGenerator result1 = this.factory.GetManagedExportsGenerator();
                ManagedExportsGenerator result2 = this.factory.GetManagedExportsGenerator();

                result1.Should().BeSameAs(result2);
            }
        }
    }
}
