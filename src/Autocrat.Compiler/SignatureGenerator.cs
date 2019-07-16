// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Generates the native signature for a managed method.
    /// </summary>
    internal class SignatureGenerator
    {
        /// <summary>
        /// Generates the native signature format for the specified method.
        /// </summary>
        /// <param name="method">Information about the method.</param>
        /// <returns>A string format representing the signature.</returns>
        public virtual string GetSignature(IMethodSymbol method)
        {
            throw new NotImplementedException();
        }
    }
}
