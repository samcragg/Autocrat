// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
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
        //// typedef std::variant<
        ////     int (*)(int),
        ////     int (*)(int, int)
        //// > method_types;
        ////
        //// std::array<method_types, 2> known_methods =
        //// {
        ////     &managed_method_1,
        ////     &managed_method_2,
        //// };
        private readonly List<(string name, string declaration)> exports =
            new List<(string, string)>();

        private readonly ISet<string> methodTypes = new HashSet<string>();

        /// <summary>
        /// Registers the method as being exported from the managed code.
        /// </summary>
        /// <param name="signature">the format of the C++ signature.</param>
        /// <param name="name">The name of the exported method.</param>
        /// <returns>The index of the method in the registration table.</returns>
        /// <remarks>
        /// The format of the signature should include a placeholder for the
        /// method name, (e.g. "int {0}(int)").
        /// </remarks>
        public virtual int RegisterMethod(string signature, string name)
        {
            this.AddMethodType(signature);
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
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen: true))
            {
                WriteHeaders(writer);
                writer.WriteLine();

                this.WriteExterns(writer);
                writer.WriteLine();

                this.WriteMethodTypes(writer);
                writer.WriteLine();

                this.WriteKnownMethods(writer);
            }
        }

        private static void WriteHeaders(StreamWriter writer)
        {
            writer.WriteLine("#include <array>");
            writer.WriteLine("#include <variant>");
        }

        private void AddExport(string signature, string name)
        {
            string declaration = string.Format(
                CultureInfo.InvariantCulture,
                signature,
                name);

            this.exports.Add((name, declaration));
        }

        private void AddMethodType(string signature)
        {
            this.methodTypes.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    signature,
                    "(*)"));
        }

        private void WriteExterns(StreamWriter writer)
        {
            foreach ((_, string declaration) in this.exports)
            {
                writer.WriteLine("extern \"C\" {0};", declaration);
            }
        }

        private void WriteKnownMethods(StreamWriter writer)
        {
            writer.WriteLine("std::array<method_types, {0}> known_methods =", this.exports.Count);
            writer.WriteLine('{');
            foreach ((string name, _) in this.exports)
            {
                writer.WriteLine("    &{0},", name);
            }

            writer.WriteLine("};");
        }

        private void WriteMethodTypes(StreamWriter writer)
        {
            writer.WriteLine("typedef std::variant<");
            foreach (string method in this.methodTypes)
            {
                writer.WriteLine("    {0},", method);
            }

            writer.WriteLine("> method_types;");
        }
    }
}
