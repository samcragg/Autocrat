// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System;

    /// <summary>
    /// Allows a delegate to be callable from the native code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Delegate)]
    public sealed class NativeDelegateAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NativeDelegateAttribute"/> class.
        /// </summary>
        /// <param name="signature">The native method signature.</param>
        public NativeDelegateAttribute(string signature)
        {
            this.Signature = signature;
        }

        /// <summary>
        /// Gets the native signature of the exported method.
        /// </summary>
        /// <remarks>
        /// This sill be passed to <c>string.Format</c> to inject the name,
        /// therefore, should be in the form of <c>return_type {0}(arg_type)</c>.
        /// </remarks>
        public string Signature { get; }
    }
}
