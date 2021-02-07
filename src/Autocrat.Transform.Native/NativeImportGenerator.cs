// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Native
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text.Json;

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
        private readonly List<(string Name, string Signature)> exports =
            new List<(string, string)>();

        /// <summary>
        /// Loads the methods exported from the managed task.
        /// </summary>
        /// <param name="json">Contains the exported JSON data.</param>
        public virtual void LoadExports(Stream json)
        {
            // Deserializing to a class doesn't allow us to validate that the
            // property is specified, hence the manual deserialize
            using var document = JsonDocument.Parse(json);
            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                string name = element.GetProperty("name").GetString();
                string signature = element.GetProperty("signature").GetString();
                this.exports.Add((name, signature));
            }
        }

        /// <summary>
        /// Writes the generated code to the specified stream.
        /// </summary>
        /// <param name="writer">Where to write the output to.</param>
        public virtual void WriteTo(TextWriter writer)
        {
            WriteHeaders(writer);
            writer.WriteLine();

            this.WriteExterns(writer);
            writer.WriteLine();

            this.WriteGetKnownMethod(writer);
        }

        private static void WriteHeaders(TextWriter writer)
        {
            writer.WriteLine("#include <array>");
            writer.WriteLine("#include <cstddef>");
            writer.WriteLine("#include <cstdint>");
            writer.WriteLine("#include \"exports.h\"");
        }

        private void WriteExterns(TextWriter writer)
        {
            foreach ((string name, string signature) in this.exports)
            {
                string declaration = string.Format(
                    CultureInfo.InvariantCulture,
                    signature,
                    name);

                writer.WriteLine("extern \"C\" {0};", declaration);
            }
        }

        private void WriteGetKnownMethod(TextWriter writer)
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
