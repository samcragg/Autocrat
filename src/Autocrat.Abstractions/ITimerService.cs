// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System;

    /// <summary>
    /// Allows the handling of timing events.
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// Registers the callback to be invoked at a regular interval.
        /// </summary>
        /// <param name="interval">The duration to wait between invocations.</param>
        /// <param name="callback">
        /// The handler to invoke when the time has elapsed.
        /// </param>
        /// <returns>A unique identifier that will be passed to the callback.</returns>
        int RegisterRepeat(TimeSpan interval, TimerCallback callback);

        /// <summary>
        /// Registers the callback to be invoked at a regular interval.
        /// </summary>
        /// <param name="delay">
        /// The amount of time to wait before calling the handler for the first
        /// time.
        /// </param>
        /// <param name="interval">The duration to wait between invocations.</param>
        /// <param name="callback">
        /// The handler to invoke when the time has elapsed.
        /// </param>
        /// <returns>A unique identifier that will be passed to the callback.</returns>
        int RegisterRepeat(TimeSpan delay, TimeSpan interval, TimerCallback callback);

        /// <summary>
        /// Registers the callback to be invoked after the specified amount of
        /// time.
        /// </summary>
        /// <param name="delay">
        /// The amount of time to wait before calling the handler.
        /// </param>
        /// <param name="callback">
        /// The handler to invoke when the time has elapsed.
        /// </param>
        /// <returns>A unique identifier that will be passed to the callback.</returns>
        int RegisterSingle(TimeSpan delay, TimerCallback callback);
    }
}
