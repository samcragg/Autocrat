// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.NativeAdapters
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
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

        /// <inheritdoc cref="IWorkerFactory.GetWorkerAsync{T}(Guid)"/>
        public static async Task<T> GetWorkerAsync<T>(Guid id)
            where T : class
        {
            IntPtr handle = NativeHelpers.GetHandle<T>();
            object? worker = LoadObjectGuid(handle, id);
            while (worker == null)
            {
                await Task.Yield();
                worker = LoadObjectGuid(handle, id);
            }

            return (T)worker;
        }

        /// <inheritdoc cref="IWorkerFactory.GetWorkerAsync{T}(long)"/>
        public static async Task<T> GetWorkerAsync<T>(long id)
            where T : class
        {
            IntPtr handle = NativeHelpers.GetHandle<T>();
            object? worker = LoadObjectInt64(handle, id);
            while (worker == null)
            {
                await Task.Yield();
                worker = LoadObjectInt64(handle, id);
            }

            return (T)worker;
        }

        /// <inheritdoc cref="IWorkerFactory.GetWorkerAsync{T}(string)"/>
        public static async Task<T> GetWorkerAsync<T>(string id)
            where T : class
        {
            IntPtr handle = NativeHelpers.GetHandle<T>();
            object? worker = LoadObjectString(handle, id);
            while (worker == null)
            {
                await Task.Yield();
                worker = LoadObjectString(handle, id);
            }

            return (T)worker;
        }

        /// <summary>
        /// Registers a method as being able to construct the specified type.
        /// </summary>
        /// <typeparam name="T">The type to construct.</typeparam>
        /// <param name="methodHandle">The method to call to construct the type.</param>
        public static void RegisterConstructor<T>(int methodHandle)
        {
            NativeMethods.RegisterConstructor(
                NativeHelpers.GetHandle<T>(),
                methodHandle);
        }

        private static unsafe object? LoadObjectGuid(IntPtr type, Guid id)
        {
            object? result = null;
            TypedReference tr = __makeref(result);
            NativeMethods.LoadObjectGuid(type, &id, &tr);
            return result;
        }

        private static unsafe object? LoadObjectInt64(IntPtr type, long id)
        {
            object? result = null;
            TypedReference tr = __makeref(result);
            NativeMethods.LoadObjectInt64(type, id, &tr);
            return result;
        }

        private static unsafe object? LoadObjectString(IntPtr type, string id)
        {
            object? result = null;
            TypedReference tr = __makeref(result);
            NativeMethods.LoadObjectString(type, NativeHelpers.GetObject(__makeref(id)), &tr);
            return result;
        }

#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members

        private static unsafe class NativeMethods
        {
            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_object_guid")]
            public static extern void LoadObjectGuid(IntPtr type, void* id, void* result);

            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_object_int64")]
            public static extern void LoadObjectInt64(IntPtr type, long id, void* result);

            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_object_string")]
            public static extern void LoadObjectString(IntPtr type, void* id, void* result);

            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "register_constructor")]
            public static extern void RegisterConstructor(IntPtr type, int methodHandle);
        }

#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
    }
}
