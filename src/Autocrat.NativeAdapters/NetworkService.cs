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
    [RewriteInterface(typeof(INetworkService))]
    public static class NetworkService
    {
        /// <summary>
        /// Calls the native method to register the callback.
        /// </summary>
        /// <param name="port">The port to register.</param>
        /// <param name="handle">The method to invoke on callback.</param>
        public static void RegisterUdp(int port, int handle)
        {
            NativeMethods.RegisterUdpDataReceived(port, handle);
        }

        private static class NativeMethods
        {
            [DllImport("*", CallingConvention = CallingConvention.Cdecl, EntryPoint = "register_udp_data_received")]
            public static extern void RegisterUdpDataReceived(int port, int handle);
        }
    }
}
