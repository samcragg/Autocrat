namespace Transform.Managed.Tests.CodeRewriting
{
    using System.Linq;
    using Autocrat.Transform.Managed.CodeRewriting;
    using FluentAssertions;
    using Mono.Cecil;
    using Xunit;

    public class WorkerFactoryVisitorTests
    {
        private readonly WorkerFactoryVisitor visitor = new WorkerFactoryVisitor();

        public sealed class VisitorTests : WorkerFactoryVisitorTests
        {
            [Fact]
            public void ShouldExtractUniqueTypes()
            {
                TypeDefinition testClass = CodeHelper.CompileType(@"
using System.Threading.Tasks;
using Autocrat.Abstractions;

public class TestClass
{
    public class Worker1 { }

    public class Worker2 { }

    public async Task ExampleMethod(IWorkerFactory factory)
    {
        await factory.GetWorkerAsync<Worker1>(0);
        await factory.GetWorkerAsync<Worker2>(0);
        await factory.GetWorkerAsync<Worker1>(0);
    }
}");

                CodeHelper.VisitMethods(this.visitor, testClass);

                this.visitor.WorkerTypes.Should().HaveCount(2);
                this.visitor.WorkerTypes.Select(x => x.FullName)
                    .Should().BeEquivalentTo("TestClass/Worker1", "TestClass/Worker2");
            }
        }
    }
}
