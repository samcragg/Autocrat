// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Autocrat.Compiler.Logging;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Loads the C# projects from disk.
    /// </summary>
    internal class ProjectLoader
    {
        private readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Gets the compilation for the specified project.
        /// </summary>
        /// <param name="references">The assembly references for the project.</param>
        /// <param name="sources">The source paths.</param>
        /// <returns>
        /// A task that represents the compilations of the projects.
        /// </returns>
        public virtual async Task<Compilation> GetCompilationAsync(string[] references, string[] sources)
        {
            static MetadataReference CreateReference(string path)
            {
                return MetadataReference.CreateFromFile(path);
            }

            var projectId = ProjectId.CreateNewId();
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            DocumentInfo[] documents =
                await this.CreateDocuments(projectId, sources).ConfigureAwait(false);

            using var workspace = new AdhocWorkspace();
            Project project = workspace.AddProject(
                ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    "project",
                    "assembly",
                    LanguageNames.CSharp,
                    compilationOptions: options,
                    documents: documents,
                    metadataReferences: references.Select(CreateReference)));

            return await project.GetCompilationAsync().ConfigureAwait(false)
                ?? throw new InvalidOperationException("Unable to compile sources");
        }

        private async Task<DocumentInfo[]> CreateDocuments(ProjectId projectId, string[] sources)
        {
            var contents = new Task<byte[]>[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                contents[i] = File.ReadAllBytesAsync(sources[i]);
            }

            this.logger.Debug("Loading source files");
            await Task.WhenAll(contents).ConfigureAwait(false);

            this.logger.Debug("Creating documents");
            var documents = new DocumentInfo[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                byte[] buffer = contents[i].Result;
                var text = TextAndVersion.Create(
                        SourceText.From(buffer, buffer.Length),
                        VersionStamp.Create());

                documents[i] = DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    sources[i],
                    loader: TextLoader.From(text));
            }

            return documents;
        }
    }
}
