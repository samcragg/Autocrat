// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    /// <summary>
    /// Represents a method that handles timer events.
    /// </summary>
    /// <param name="token">The token returned when registering the callback.</param>
    [NativeDelegate("void {0}(std::int32_t)")]
    public delegate void TimerCallback(int token);
}
