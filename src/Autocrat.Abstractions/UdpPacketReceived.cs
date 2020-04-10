// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Called when a UDP packet is received.
    /// </summary>
    /// <param name="port">The port the data was received on.</param>
    /// <param name="data">The raw bytes received.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [NativeDelegate("void* {0}(std::int32_t, const void*)")]
    public delegate Task UdpPacketReceived(int port, byte[] data);
}
