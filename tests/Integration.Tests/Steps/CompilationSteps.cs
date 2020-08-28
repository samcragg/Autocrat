namespace Integration.Tests.Steps
{
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using FluentAssertions;
    using TechTalk.SpecFlow;
    using Xunit.Abstractions;

    [Binding]
    public class CompilationSteps
    {
        // dotnet restore has some weirdness about specifying the web sources
        // via the command options (see https://stackoverflow.com/q/56231780/312325)
        // so we'll use the RestoreSources property to specify our locally built
        // packages.
        private const string MinimalProjectFormat = @"<Project Sdk='Microsoft.NET.Sdk'>
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyName>NativeProgram</AssemblyName>
    <LangVersion>latest</LangVersion>
    <NoWarn>NU1604</NoWarn> <!-- Project dependency ... does not contain an inclusive lower bound. -->
    <RestoreSources>{0}</RestoreSources>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include='Autocrat.Abstractions' />
    <PackageReference Include='Autocrat.Compiler' PrivateAssets='all' />

    <None Update='*.json'>
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>";

        private static TempDirectory PackageDirectory;
        private readonly CompilationContext compilationContext;
        private readonly ITestOutputHelper logger;
        private readonly string packageSources;
        private int fileCount;
        private string nativeOutput;

        public CompilationSteps(ITestOutputHelper logger, CompilationContext compilationContext)
        {
            this.logger = logger;
            this.compilationContext = compilationContext;

            // The packages are in build/packages
            // The current directory is something like tests/Integration.Tests/bin/Debug/netcoreapp3.1
            string buildPackages = Path.Combine(
                Directory.GetCurrentDirectory().Split("Integration.Tests")[0],
                "..",
                "build",
                "packages");

            this.packageSources = buildPackages + @"; https://api.nuget.org/v3/index.json";
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            PackageDirectory.Dispose();
        }

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            PackageDirectory = new TempDirectory();
        }

        [When(@"I compile the code")]
        public void CompileTheCode()
        {
            File.WriteAllText(
                Path.Combine(this.compilationContext.ProjectDirectory, "Project.csproj"),
                string.Format(MinimalProjectFormat, this.packageSources));

            this.RunProcess("dotnet", $"restore -v:q --no-cache --packages \"{PackageDirectory}\"")
                .Should().Be(0);

            this.RunProcess("dotnet", $"publish -v:q --nologo --no-restore -o {this.compilationContext.OutputDirectory}")
                .Should().Be(0);
        }

        [Given(@"I have the following code")]
        public void CreateFileForCode(string code)
        {
            this.fileCount++;
            File.WriteAllText(
                Path.Combine(this.compilationContext.ProjectDirectory, $"Code{this.fileCount}.cs"),
                code);
        }

        [When(@"I run the native program")]
        public void RunTheNativeProgram()
        {
            var output = new StringBuilder();
            Process process = this.CreateProcess(this.compilationContext.NativeProgram, "");
            process.OutputDataReceived += (_, e) => output.AppendLine(e.Data ?? string.Empty);

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            this.nativeOutput = output.ToString();
        }

        [Then(@"it's output should contain ""(.*)""")]
        public void VerifyOutputContains(string output)
        {
            this.nativeOutput.Should().ContainEquivalentOf(output);
        }

        private Process CreateProcess(string command, string arguments)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    Arguments = arguments,
                    FileName = command,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = this.compilationContext.ProjectDirectory,
                }
            };
        }

        private int RunProcess(string command, string arguments)
        {
            Process process = this.CreateProcess(command, arguments);
            process.ErrorDataReceived += (_, e) => this.logger.WriteLine(e.Data ?? string.Empty);
            process.OutputDataReceived += (_, e) => this.logger.WriteLine(e.Data ?? string.Empty);

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
