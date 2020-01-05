// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    /// <summary>
    /// Allows the creation of worker objects.
    /// </summary>
    public interface IWorkerFactory
    {
        /// <summary>
        /// Gets a worker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <returns>An instance of the specified type.</returns>
        public T GetWorker<T>()
            where T : class;
    }
}
