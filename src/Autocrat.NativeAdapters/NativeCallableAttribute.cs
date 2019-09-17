// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Marks a managed method as being callable from native code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NativeCallableAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the calling convention required to call the method.
        /// </summary>
        public CallingConvention CallingConvention { get; set; }

        /// <summary>
        /// Gets or sets the name of the method for native code.
        /// </summary>
        public string EntryPoint { get; set; }
    }
}
