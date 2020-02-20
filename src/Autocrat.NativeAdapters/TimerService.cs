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
    /// Allows the handling of timing events.
    /// </summary>
    [RewriteInterface(typeof(ITimerService))]
    public static class TimerService
    {
        // The docs for TimeSpan state a tick represents 100 nanoseconds
        private const long TicksPerMicrosecond = 10;

        /// <summary>
        /// Registers the callback to be invoked at a regular interval.
        /// </summary>
        /// <param name="interval">The duration to wait between invocations.</param>
        /// <param name="handle">The method to invoke on callback.</param>
        /// <returns>A unique identifier that will be passed to the callback.</returns>
        public static int RegisterRepeat(TimeSpan interval, int handle)
        {
            return RegisterRepeat(interval, interval, handle);
        }

        /// <summary>
        /// Registers the callback to be invoked at a regular interval.
        /// </summary>
        /// <param name="delay">
        /// The amount of time to wait before calling the handler for the first
        /// time.
        /// </param>
        /// <param name="interval">The duration to wait between invocations.</param>
        /// <param name="handle">The method to invoke on callback.</param>
        /// <returns>A unique identifier that will be passed to the callback.</returns>
        public static int RegisterRepeat(TimeSpan delay, TimeSpan interval, int handle)
        {
            return NativeMethods.RegisterTimer(
                delay.Ticks / TicksPerMicrosecond,
                interval.Ticks / TicksPerMicrosecond,
                handle);
        }

        /// <summary>
        /// Registers the callback to be invoked after the specified amount of
        /// time.
        /// </summary>
        /// <param name="delay">
        /// The amount of time to wait before calling the handler.
        /// </param>
        /// <param name="handle">The method to invoke on callback.</param>
        /// <returns>A unique identifier that will be passed to the callback.</returns>
        public static int RegisterSingle(TimeSpan delay, int handle)
        {
            return NativeMethods.RegisterTimer(
                delay.Ticks / TicksPerMicrosecond,
                0,
                handle);
        }

        private static class NativeMethods
        {
            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "register_timer")]
            public static extern int RegisterTimer(long delayUs, long intervalUs, int handle);
        }
    }
}
