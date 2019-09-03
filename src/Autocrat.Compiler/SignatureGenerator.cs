// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Generates the native signature for a managed method.
    /// </summary>
    internal class SignatureGenerator
    {
        private readonly StringBuilder buffer = new StringBuilder();

        private readonly Dictionary<string, string> typeMappings =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "byte[]", "void*" },
                { "double", "double" },
                { "float", "float" },
                { "int", "std::int32_t" },
                { "long", "std::int64_t" },
                { "uint", "std::uint32_t" },
                { "ulong", "std::uint64_t" },
                { "void", "void" },
            };

        /// <summary>
        /// Generates the native signature format for the specified method.
        /// </summary>
        /// <param name="method">Information about the method.</param>
        /// <returns>A string format representing the signature.</returns>
        public virtual string GetSignature(IMethodSymbol method)
        {
            this.buffer.Clear();
            this.AppendType(method.ReturnType);
            this.buffer.Append(" {0}(");

            for (int i = 0; i < method.Parameters.Length; i++)
            {
                if (i != 0)
                {
                    this.buffer.Append(", ");
                }

                this.AppendType(method.Parameters[i].Type);
            }

            this.buffer.Append(')');
            return this.buffer.ToString();
        }

        private void AppendType(ITypeSymbol type)
        {
            this.buffer.Append(this.typeMappings[type.ToDisplayString()]);
        }
    }
}
