// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed.CodeGeneration
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Generates the native code that calls to the exported managed methods.
    /// </summary>
    internal class NativeImportGenerator
    {
        // This class generates C++ code like the following:
        //
        //// extern "C" int managed_method_1(int);
        //// extern "C" int managed_method_2(int, int);
        ////
        //// method_types& get_known_method(std::size_t handle)
        //// {
        ////     static std::array<method_types, 2> known_methods =
        ////     {
        ////         &managed_method_1,
        ////         &managed_method_2,
        ////     };
        ////     return known_methods.at(handle);
        //// }
        private readonly List<(string Name, string Declaration)> exports =
            new List<(string, string)>();

        /// <summary>
        /// Registers the method as being exported from the managed code.
        /// </summary>
        /// <param name="signature">The format of the C++ signature.</param>
        /// <param name="name">The name of the exported method.</param>
        /// <returns>The index of the method in the registration table.</returns>
        /// <remarks>
        /// The format of the signature should include a placeholder for the
        /// method name, (e.g. "int {0}(int)").
        /// </remarks>
        public virtual int RegisterMethod(string signature, string name)
        {
            int index = this.exports.Count;
            this.AddExport(signature, name);
            return index;
        }

        /// <summary>
        /// Writes the generated code to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to output to.</param>
        public virtual void WriteTo(Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen: true);
            WriteHeaders(writer);
            writer.WriteLine();

            this.WriteExterns(writer);
            writer.WriteLine();

            this.WriteGetKnownMethod(writer);
        }

        private static void WriteHeaders(StreamWriter writer)
        {
            writer.WriteLine("#include <array>");
            writer.WriteLine("#include <cstddef>");
            writer.WriteLine("#include <cstdint>");
            writer.WriteLine("#include \"exports.h\"");
        }

        private void AddExport(string signature, string name)
        {
            string declaration = string.Format(
                CultureInfo.InvariantCulture,
                signature,
                name);

            this.exports.Add((name, declaration));
        }

        private void WriteExterns(StreamWriter writer)
        {
            foreach ((_, string declaration) in this.exports)
            {
                writer.WriteLine("extern \"C\" {0};", declaration);
            }
        }

        private void WriteGetKnownMethod(StreamWriter writer)
        {
            writer.WriteLine("method_types& get_known_method(std::size_t handle)");
            writer.WriteLine('{');
            writer.WriteLine("    static std::array<method_types, {0}> known_methods =", this.exports.Count);
            writer.WriteLine("    {");
            foreach ((string name, _) in this.exports)
            {
                writer.WriteLine("        &{0},", name);
            }

            writer.WriteLine("    };");
            writer.WriteLine("    return known_methods.at(handle);");
            writer.WriteLine('}');
        }
    }
}
