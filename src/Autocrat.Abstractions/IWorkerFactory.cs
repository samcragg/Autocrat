// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the creation of worker objects.
    /// </summary>
    public interface IWorkerFactory
    {
        /// <summary>
        /// Gets a worker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="id">The identifier of the worker.</param>
        /// <returns>A task that represents an instance of the specified type.</returns>
        public Task<T> GetWorkerAsync<T>(Guid id)
            where T : class;

        /// <summary>
        /// Gets a worker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="id">The identifier of the worker.</param>
        /// <returns>A task that represents an instance of the specified type.</returns>
        public Task<T> GetWorkerAsync<T>(long id)
            where T : class;

        /// <summary>
        /// Gets a worker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="id">The identifier of the worker.</param>
        /// <returns>A task that represents an instance of the specified type.</returns>
        public Task<T> GetWorkerAsync<T>(string id)
            where T : class;
    }
}
