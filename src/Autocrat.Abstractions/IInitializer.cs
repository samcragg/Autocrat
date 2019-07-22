// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    /// <summary>
    /// Allows the initialization of the application logic.
    /// </summary>
    public interface IInitializer
    {
        /// <summary>
        /// Called when the configuration has been loaded.
        /// </summary>
        /// <remarks>
        /// This method can be called after the initial startup if changes to
        /// the configuration files are detected.
        /// </remarks>
        void OnConfigurationLoaded();
    }
}
