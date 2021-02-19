// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Native
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Generates code to initialize the managed static data.
    /// </summary>
    internal class StaticInitializerGenerator
    {
        // This class generates C++ code like the following:
        //
        //// extern "C" void __GetGCStaticBase_Namespace_Class();
        //// extern "C" void __GetNonGCStaticBase_Namespace_Class();
        ////
        //// void initialize_statics()
        //// {
        ////     __GetGCStaticBase_Namespace_Class();
        ////     __GetNonGCStaticBase_Namespace_Class();
        //// }
        private const string AliasMacro = @"
#ifdef __GNUC__
#define ALIAS_METHOD(name, symbol) name() asm(#symbol);
#else
#define ALIAS_METHOD(name, symbol) name(); __pragma(comment(linker, ""/alternatename:"" #name ""="" #symbol))
#endif
";

        private readonly HashSet<string> methodStubs = new HashSet<string>();

        /// <summary>
        /// Loads the information from the specified file.
        /// </summary>
        /// <param name="mapFile">The path to the map file.</param>
        public virtual void Load(Stream mapFile)
        {
            using var reader = XmlReader.Create(mapFile);
            reader.ReadStartElement("ObjectNodes");
            this.methodStubs.UnionWith(GetStaticBaseHelpers(reader));
        }

        /// <summary>
        /// Writes the generated code to the specified stream.
        /// </summary>
        /// <param name="writer">Where to write the output to.</param>
        public virtual void WriteTo(TextWriter writer)
        {
            writer.WriteLine(AliasMacro);
            string[] methodNames = this.WriteExterns(writer);
            writer.WriteLine();
            WriteInitializeStaticMethod(writer, methodNames);
        }

        private static bool EscapeMethodName(string name, out string escapedName)
        {
            if (name.IndexOfAny(new[] { '<', '>' }) < 0)
            {
                escapedName = name;
                return false;
            }
            else
            {
                var builder = new StringBuilder(name);
                builder.Replace('<', '_')
                       .Replace('>', '_');
                escapedName = builder.ToString();
                return true;
            }
        }

        private static IEnumerable<string> GetStaticBaseHelpers(XmlReader reader)
        {
            while (reader.Read())
            {
                if ((reader.NodeType == XmlNodeType.Element) &&
                    string.Equals("ReadyToRunHelper", reader.Name, StringComparison.Ordinal))
                {
                    string name = reader.GetAttribute("Name");
                    if (name.StartsWith("__GetGCStaticBase", StringComparison.Ordinal) ||
                        name.StartsWith("__GetNonGCStaticBase", StringComparison.Ordinal))
                    {
                        yield return name;
                    }
                }
            }
        }

        private static void WriteInitializeStaticMethod(TextWriter writer, string[] methodNames)
        {
            writer.WriteLine("void initialize_statics()");
            writer.WriteLine('{');
            foreach (string method in methodNames)
            {
                writer.WriteLine("    {0}();", method);
            }

            writer.WriteLine('}');
        }

        private string[] WriteExterns(TextWriter writer)
        {
            string[] methodNames = new string[this.methodStubs.Count];
            int index = 0;
            foreach (string method in this.methodStubs)
            {
                if (EscapeMethodName(method, out string escapedName))
                {
                    writer.WriteLine("extern \"C\" void ALIAS_METHOD({0}, \"{1}\")", escapedName, method);
                }
                else
                {
                    writer.WriteLine("extern \"C\" void {0}();", method);
                }

                methodNames[index++] = escapedName;
            }

            return methodNames;
        }
    }
}
