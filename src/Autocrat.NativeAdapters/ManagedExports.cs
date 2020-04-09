// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.NativeAdapters
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// Contains helper method for native code to access managed information.
    /// </summary>
    internal static class ManagedExports
    {
        /// <summary>
        /// Gets the handle for the byte array type.
        /// </summary>
        /// <returns>A pointer to a native type handle.</returns>
        [NativeCallable(EntryPoint = "GetByteArrayType", CallingConvention = CallingConvention.Cdecl)]
        public static IntPtr GetByteArrayType()
        {
            return typeof(byte[]).TypeHandle.Value;
        }

        /// <summary>
        /// Initialize managed resources for the current thread.
        /// </summary>
        [NativeCallable(EntryPoint = "InitializeManagedThread", CallingConvention = CallingConvention.Cdecl)]
        public static void InitializeManagedThread()
        {
            SynchronizationContext.SetSynchronizationContext(
                new TaskServiceSynchronizationContext());
        }
    }
}
