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
        /// <summary>
        /// Gets a worker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <returns>An instance of the specified type.</returns>
        public static unsafe T GetWorker<T>()
            where T : class
        {
            // Native code can't pass back an object, as it doesn't know how to
            // marshal it. Instead, pass a reference to our local variable, so
            // the native code can change where it points to.
            object? result = null;
            TypedReference tr = __makeref(result);
            NativeMethods.LoadObject(GetHandle<T>(), &tr);
            return (T)result!;
        }

        /// <summary>
        /// Registers a method as being able to construct the specified type.
        /// </summary>
        /// <typeparam name="T">The type to construct.</typeparam>
        /// <param name="methodHandle">The method to call to construct the type.</param>
        public static void RegisterConstructor<T>(int methodHandle)
        {
            NativeMethods.RegisterObjectConstructor(GetHandle<T>(), methodHandle);
        }

        private static IntPtr GetHandle<T>()
        {
            return typeof(T).TypeHandle.Value;
        }

        private static unsafe class NativeMethods
        {
            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_object")]
            public static extern void LoadObject(IntPtr type, void* result);

            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "register_constructor")]
            public static extern void RegisterObjectConstructor(IntPtr type, int methodHandle);
        }
    }
}
