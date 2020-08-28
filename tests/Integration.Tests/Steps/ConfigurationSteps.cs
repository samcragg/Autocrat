namespace Integration.Tests.Steps
{
    using System.IO;
    using TechTalk.SpecFlow;

    [Binding]
    public class ConfigurationSteps
    {
        private readonly CompilationContext compilationContext;

        public ConfigurationSteps(CompilationContext compilationContext)
        {
            this.compilationContext = compilationContext;
        }

        [Given(@"I have the following configuration")]
        public void CreateConfigFile(string json)
        {
            File.WriteAllText(
                Path.Combine(this.compilationContext.ProjectDirectory, "config.json"),
                json);
        }
    }
}
