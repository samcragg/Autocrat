// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;

    /// <summary>
    /// Maintains a collection of methods exported from the managed code.
    /// </summary>
    internal class ExportedMethods
    {
        private readonly List<object> exports = new List<object>();

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
            this.exports.Add(new { name, signature });
            return index;
        }

        /// <summary>
        /// Serializes the exported methods to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to output to.</param>
        public virtual void SerializeTo(Stream stream)
        {
            using var writer = new Utf8JsonWriter(stream);
            JsonSerializer.Serialize(writer, this.exports);
        }
    }
}
