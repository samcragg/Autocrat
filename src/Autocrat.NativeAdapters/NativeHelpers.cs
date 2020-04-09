// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.NativeAdapters
{
    using System;

    /// <summary>
    /// Contains helper methods for interacting with the native methods.
    /// </summary>
    internal static unsafe class NativeHelpers
    {
        /// <summary>
        /// Gets a pointer to the type information.
        /// </summary>
        /// <typeparam name="T">The type to get the information of.</typeparam>
        /// <returns>A pointer to the type information.</returns>
        public static IntPtr GetHandle<T>()
        {
            return typeof(T).TypeHandle.Value;
        }

        /// <summary>
        /// Gets a pointer to the managed object.
        /// </summary>
        /// <param name="reference">A reference to the object.</param>
        /// <returns>A pointer to the managed object.</returns>
        public static void* GetObject(TypedReference reference)
        {
            // The first field of TypedReference is the pointer to the object,
            // hence the weird cast
            return **(void***)&reference;
        }
    }
}
