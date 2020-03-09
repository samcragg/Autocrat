// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System;

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
        /// <returns>An instance of the specified type.</returns>
        public T GetWorker<T>(Guid id)
            where T : class;

        /// <summary>
        /// Gets a worker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="id">The identifier of the worker.</param>
        /// <returns>An instance of the specified type.</returns>
        public T GetWorker<T>(long id)
            where T : class;

        /// <summary>
        /// Gets a worker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="id">The identifier of the worker.</param>
        /// <returns>An instance of the specified type.</returns>
        public T GetWorker<T>(string id)
            where T : class;
    }
}
