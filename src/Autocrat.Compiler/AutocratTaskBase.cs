// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Autocrat.Common;

    /// <summary>
    /// Provides a generic MSBuild task.
    /// </summary>
    public abstract class AutocratTaskBase : Microsoft.Build.Utilities.Task
    {
        /// <inheritdoc />
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the main entry point.")]
        public override bool Execute()
        {
            try
            {
                LogManager.SetLogger(new MSBuildLogger(this.Log));
                return this.Transform();
            }
            catch (Exception ex)
            {
                this.Log.LogErrorFromException(ex);
                return false;
            }
        }

        /// <summary>
        /// Performs the transformation logic.
        /// </summary>
        /// <returns><c>true</c> if successful; otherwise, <c>false</c>.</returns>
        protected abstract bool Transform();
    }
}
