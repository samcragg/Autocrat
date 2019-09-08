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
        /// Registers the specified type as handling UDP data.
        /// </summary>
        /// <typeparam name="T">The class type to create.</typeparam>
        /// <param name="port">The port number to listen on.</param>
        void RegisterUdp<T>(int port)
            where T : IUdpHandler;
    }
}
