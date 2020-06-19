namespace Integration.Tests
{
    using System;
    using System.IO;

    internal sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            this.FullName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(this.FullName);
        }

        public string FullName { get; }

        public static implicit operator string(TempDirectory dir)
        {
            return dir.FullName;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(this.FullName, recursive: true);
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            return this.FullName;
        }
    }
}
