// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System;
    using System.ComponentModel;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Contains methods for comparing strings in a case-insensitive way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Internal class used by the compiler and runtime to generate classes to
    /// deserialize configuration properties.
    /// </para><para>
    /// This class only takes into account uppercase ASCII letters (i.e. doesn't
    /// follow the Unicode definition of uppercase/lowercase).
    /// </para>
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class CaseInsensitiveStringHelper
    {
        /// <summary>
        /// Indicates whether two strings are equal.
        /// </summary>
        /// <param name="value">An uppercase string to compare to.</param>
        /// <param name="other">
        /// Contains the value to compare to the string.
        /// </param>
        /// <returns>
        /// <c>true</c> if the buffer equals the specified string; otherwise
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The string passing in as <c>value</c> MUST already be uppercase.
        /// </remarks>
        public static bool Equals(string value, ReadOnlySpan<byte> other)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Assert(value.ToUpperInvariant() == value, "value must be uppercase");
            if (value.Length != other.Length)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                // Make uppercase
                uint c = other[i];
                if ((c - 'a') <= ('z' - 'a'))
                {
                    c -= 'a' - 'A';
                }

                if (c != value[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the hash code for the specified string.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>A 32-bit signed integer calculated from the string.</returns>
        public static int GetHashCode(ReadOnlySpan<byte> value)
        {
            // Fowler–Noll–Vo hash function
            // https://en.wikipedia.org/wiki/Fowler–Noll–Vo_hash_function
            const uint FnvOffsetBasis = 2166136261;
            const uint FnvPrime = 16777619;

            uint hash = FnvOffsetBasis;
            for (int i = 0; i < value.Length; i++)
            {
                // Make uppercase
                uint c = value[i];
                if ((c - 'a') <= ('z' - 'a'))
                {
                    c -= 'a' - 'A';
                }

                hash = (hash ^ c) * FnvPrime;
            }

            return (int)hash;
        }
    }
}
