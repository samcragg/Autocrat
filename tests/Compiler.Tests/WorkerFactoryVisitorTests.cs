namespace Compiler.Tests
{
    using System.Linq;
    using Autocrat.Compiler;
    using FluentAssertions;
    using Microsoft.CodeAnalysis;
    using Xunit;

    public class WorkerFactoryVisitorTests
    {
        public sealed class WorkerTypes : WorkerFactoryVisitorTests
        {
            [Fact]
            public void ShouldExtractUniqueTypes()
            {
                Compilation compilation = CompilationHelper.CompileCode(@"
namespace Autocrat.Abstractions
{
    public interface IWorkerFactory
    {
        void GetWorker<T>();
    }
}

namespace TestNamespace
{
    using Autocrat.Abstractions;

    public class Worker1 { }

    public class Worker2 { }

    public class ExampleClass
    {
        public void ExampleMethod(IWorkerFactory factory)
        {
            factory.GetWorker<Worker1>();
            factory.GetWorker<Worker2>();
            factory.GetWorker<Worker1>();
        }
    }
}");

                var visitor = new WorkerFactoryVisitor(compilation);

                visitor.WorkerTypes.Should().HaveCount(2);
                visitor.WorkerTypes.Select(x => x.ToDisplayString())
                       .Should().BeEquivalentTo("TestNamespace.Worker1", "TestNamespace.Worker2");
            }
        }
    }
}
