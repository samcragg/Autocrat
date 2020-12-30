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
        [UnmanagedCallersOnly(EntryPoint = nameof(GetByteArrayType), CallingConvention = CallingConvention.Cdecl)]
        public static IntPtr GetByteArrayType()
        {
            return typeof(byte[]).TypeHandle.Value;
        }

        /// <summary>
        /// Initialize managed resources for the current thread.
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = nameof(InitializeManagedThread), CallingConvention = CallingConvention.Cdecl)]
        public static void InitializeManagedThread()
        {
            SynchronizationContext.SetSynchronizationContext(
                new TaskServiceSynchronizationContext());
        }

        /// <summary>
        /// Loads the configuration data.
        /// </summary>
        /// <param name="source">The configuration data.</param>
        /// <returns>
        /// <c>true</c> if the configuration was correctly parsed; otherwise,
        /// <c>false</c>.
        /// </returns>
        [UnmanagedCallersOnly(EntryPoint = nameof(LoadConfiguration), CallingConvention = CallingConvention.Cdecl)]
        public static unsafe bool LoadConfiguration(void* source)
        {
            return ConfigService.Load(NativeHelpers.FromPointer<byte[]>(source));
        }
    }
}
