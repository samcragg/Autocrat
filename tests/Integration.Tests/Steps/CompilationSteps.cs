namespace Integration.Tests.Steps
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using FluentAssertions;
    using TechTalk.SpecFlow;
    using Xunit.Abstractions;

    [Binding]
    public class CompilationSteps : IDisposable
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
  </ItemGroup>
</Project>";

        private static TempDirectory PackageDirectory;
        private readonly ITestOutputHelper logger;
        private readonly string nativeProgram;
        private readonly string packageSources;
        private readonly TempDirectory projectDirectory;
        private int fileCount;
        private string nativeOutput;

        public CompilationSteps(ITestOutputHelper logger)
        {
            this.logger = logger;

            // The packages are in build/packages
            // The current directory is something like tests/Integration.Tests/bin/Debug/netcoreapp3.1
            string buildPackages = Path.Combine(
                Directory.GetCurrentDirectory().Split("Integration.Tests")[0],
                "..",
                "build",
                "packages");

            this.packageSources = buildPackages + @"; https://api.nuget.org/v3/index.json";
            this.projectDirectory = new TempDirectory();
            this.nativeProgram = Path.Combine(this.projectDirectory, "output", "NativeProgram");
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
                Path.Combine(this.projectDirectory, "Project.csproj"),
                string.Format(MinimalProjectFormat, this.packageSources));

            this.RunProcess("dotnet", $"restore -v:q --no-cache --packages \"{PackageDirectory}\"")
                .Should().Be(0);

            string outputDir = Path.GetDirectoryName(this.nativeProgram);
            this.RunProcess("dotnet", $"publish -v:q --nologo --no-restore -o {outputDir}")
                .Should().Be(0);
        }

        [Given(@"I have the following code")]
        public void CreateFileForCode(string code)
        {
            this.fileCount++;
            File.WriteAllText(
                Path.Combine(this.projectDirectory, $"Code{this.fileCount}.cs"),
                code);
        }

        public void Dispose()
        {
            this.projectDirectory.Dispose();
        }

        [When(@"I run the native program")]
        public void RunTheNativeProgram()
        {
            var output = new StringBuilder();
            Process process = this.CreateProcess(this.nativeProgram, "");
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
                    WorkingDirectory = this.projectDirectory,
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
