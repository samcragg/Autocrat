namespace Integration.Tests.Steps
{
    using TechTalk.SpecFlow;

    [Binding]
    public class ArgumentParsingSteps
    {
        private readonly CompilationContext compilationContext;

        public ArgumentParsingSteps(CompilationContext compilationContext)
        {
            this.compilationContext = compilationContext;
        }

        [Given(@"the project description is ""(.*)""")]
        public void SetTheProjectDescription(string description)
        {
            this.compilationContext.Description = description;
        }

        [Given(@"the project version is ""(.*)""")]
        public void SetTheProjectVersion(string version)
        {
            this.compilationContext.Version = version;
        }
    }
}
