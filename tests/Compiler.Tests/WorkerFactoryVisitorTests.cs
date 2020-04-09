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
using System.Threading.Tasks;

namespace Autocrat.Abstractions
{
    public interface IWorkerFactory
    {
        Task<T> GetWorkerAsync<T>();
    }
}

namespace TestNamespace
{
    using Autocrat.Abstractions;

    public class Worker1 { }

    public class Worker2 { }

    public class ExampleClass
    {
        public async Task ExampleMethod(IWorkerFactory factory)
        {
            await factory.GetWorkerAsync<Worker1>();
            await factory.GetWorkerAsync<Worker2>();
            await factory.GetWorkerAsync<Worker1>();
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
