// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Native
{
    using System.IO;

    /// <summary>
    /// Creates the generated code for the main entry point of the application.
    /// </summary>
    internal class MainGenerator
    {
        /// <summary>
        /// Gets or sets the description of the program.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the version of the program.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Writes the generated code to the specified stream.
        /// </summary>
        /// <param name="writer">Where to write the output to.</param>
        public virtual void WriteTo(TextWriter writer)
        {
            writer.WriteLine("int main(int argc, char* argv[])");
            writer.WriteLine("{");

            EmitCallMethod(writer, "set_description", this.Description);
            EmitCallMethod(writer, "set_version", this.Version);

            writer.WriteLine("    return autocrat_main(argc, argv);");
            writer.WriteLine("}");
        }

        private static void EmitCallMethod(TextWriter writer, string method, string? argument)
        {
            if (!string.IsNullOrEmpty(argument))
            {
                writer.Write("    " + method + "(\"");
                writer.Write(argument);
                writer.WriteLine("\");");
            }
        }
    }
}
