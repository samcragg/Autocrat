// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.NativeAdapters
{
    using System;
    using System.Runtime.InteropServices;
    using Autocrat.Abstractions;

    /// <summary>
    /// Allows the creation of worker objects.
    /// </summary>
    [RewriteInterface(typeof(IWorkerFactory))]
    public static class WorkerFactory
    {
        // Native code can't pass back an object, as it doesn't know how to
        // marshal it. Therefore, the GetWorker methods pass a reference to
        // their local variable so the native code can change that instead.

        /// <inheritdoc cref="IWorkerFactory.GetWorker{T}(Guid)"/>
        public static unsafe T GetWorker<T>(Guid id)
            where T : class
        {
            object? result = null;
            TypedReference tr = __makeref(result);
            NativeMethods.LoadObjectGuid(NativeHelpers.GetHandle<T>(), &id, &tr);
            return (T)result!;
        }

        /// <inheritdoc cref="IWorkerFactory.GetWorker{T}(long)"/>
        public static unsafe T GetWorker<T>(long id)
            where T : class
        {
            object? result = null;
            TypedReference tr = __makeref(result);
            NativeMethods.LoadObjectInt64(NativeHelpers.GetHandle<T>(), id, &tr);
            return (T)result!;
        }

        /// <inheritdoc cref="IWorkerFactory.GetWorker{T}(string)"/>
        public static unsafe T GetWorker<T>(string id)
            where T : class
        {
            object? result = null;
            TypedReference tr = __makeref(result);
            NativeMethods.LoadObjectString(
                NativeHelpers.GetHandle<T>(),
                NativeHelpers.GetObject(__makeref(id)),
                &tr);
            return (T)result!;
        }

        /// <summary>
        /// Registers a method as being able to construct the specified type.
        /// </summary>
        /// <typeparam name="T">The type to construct.</typeparam>
        /// <param name="methodHandle">The method to call to construct the type.</param>
        public static void RegisterConstructor<T>(int methodHandle)
        {
            NativeMethods.RegisterObjectConstructor(
                NativeHelpers.GetHandle<T>(),
                methodHandle);
        }

        private static unsafe class NativeMethods
        {
            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_object_guid")]
            public static extern void LoadObjectGuid(IntPtr type, void* id, void* result);

            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_object_int64")]
            public static extern void LoadObjectInt64(IntPtr type, long id, void* result);

            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_object_string")]
            public static extern void LoadObjectString(IntPtr type, void* id, void* result);

            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "register_constructor")]
            public static extern void RegisterObjectConstructor(IntPtr type, int methodHandle);
        }
    }
}
