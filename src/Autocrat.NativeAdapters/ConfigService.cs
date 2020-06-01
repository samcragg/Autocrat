// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.NativeAdapters
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json;

    /// <summary>
    /// Responsible for the loaded of configuration data.
    /// </summary>
    public static class ConfigService
    {
        private static LoadConfiguration? loadCallback;

        /// <summary>
        /// Represents a method to call to read the configuration.
        /// </summary>
        /// <param name="reader">Contains the configuration.</param>
        public delegate void LoadConfiguration(ref Utf8JsonReader reader);

        /// <summary>
        /// Initializes the configuration service.
        /// </summary>
        /// <param name="callback">The method to call with the parsed configuration.</param>
        public static void Initialize(LoadConfiguration callback)
        {
            loadCallback = callback;
        }

        /// <summary>
        /// Loads the configuration data.
        /// </summary>
        /// <param name="source">The configuration data.</param>
        /// <returns>
        /// <c>true</c> if the data was successfully loaded; otherwise, <c>false</c>.
        /// </returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We can't propagate the exception to native code.")]
        public static bool Load(byte[] source)
        {
            if (loadCallback != null)
            {
                try
                {
                    var options = new JsonReaderOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip,
                    };
                    var reader = new Utf8JsonReader(source, options);
                    loadCallback(ref reader);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
    }
}
