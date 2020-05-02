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
    /// Synchronizes work to the native task service.
    /// </summary>
    internal sealed class TaskServiceSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// Starts a new action on the task scheduler.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public static unsafe void StartNew(Action action)
        {
            NativeMethods.TaskStartNew(NativeHelpers.GetObject(__makeref(action)));
        }

        /// <inheritdoc />
        public override SynchronizationContext CreateCopy()
        {
            // We're stateless
            return this;
        }

        /// <inheritdoc />
        public unsafe override void Post(SendOrPostCallback d, object state)
        {
            NativeMethods.TaskEnqueue(
                NativeHelpers.GetObject(__makeref(d)),
                NativeHelpers.GetObject(__makeref(state)));
        }

        private static unsafe class NativeMethods
        {
            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "task_enqueue")]
            public static extern void TaskEnqueue(void* callback, void* state);

            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "task_start_new")]
            public static extern void TaskStartNew(void* action);
        }
    }
}
