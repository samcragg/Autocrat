// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.NativeAdapters
{
    using System.Runtime.InteropServices;
    using Autocrat.Abstractions;

    /// <summary>
    /// Allows the registering of low level network resource handling.
    /// </summary>
    [NativeAdapter]
    public class NetworkService : INetworkService
    {
        /// <summary>
        /// Calls the native method to register the callback.
        /// </summary>
        /// <param name="port">The port to register.</param>
        /// <param name="handle">The method to invoke on callback.</param>
        [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "register_udp_data_received")]
        public static extern void OnDataReceived(int port, int handle);

        /// <inheritdoc />
        public void RegisterUdp<T>(int port)
            where T : IUdpHandler
        {
            // Not used at runtime
        }
    }
}
