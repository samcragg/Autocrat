// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    /// <summary>
    /// Allows the handling of low level network resources.
    /// </summary>
    public interface INetworkService
    {
        /// <summary>
        /// Registers the handler for UDP packets on the specified port.
        /// </summary>
        /// <param name="port">The port number to listen on.</param>
        /// <param name="callback">The handler to invoke when data is received.</param>
        void RegisterUdp(int port, UdpPacketReceived callback);
    }
}
