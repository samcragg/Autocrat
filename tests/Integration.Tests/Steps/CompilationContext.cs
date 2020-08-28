namespace Integration.Tests.Steps
{
    using System;
    using System.IO;

    public class CompilationContext : IDisposable
    {
        private readonly TempDirectory projectDirectory;

        public CompilationContext()
        {
            this.projectDirectory = new TempDirectory();

            this.OutputDirectory = Path.Combine(
                this.projectDirectory.FullName,
                "output");

            this.NativeProgram = Path.Combine(
                this.OutputDirectory,
                "NativeProgram");
        }

        public string NativeProgram { get; }

        public string OutputDirectory { get; }

        public string ProjectDirectory => this.projectDirectory.FullName;

        public void Dispose()
        {
            this.projectDirectory.Dispose();
        }
    }
}
