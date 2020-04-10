// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a method that handles timer events.
    /// </summary>
    /// <param name="token">The token returned when registering the callback.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [NativeDelegate("void* {0}(std::int32_t)")]
    public delegate Task TimerCallback(int token);
}
