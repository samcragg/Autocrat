// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.NativeAdapters
{
    using System;

    /// <summary>
    /// Marks a class as implementing and interface that should be rewritten to
    /// call native methods instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class NativeAdapterAttribute : Attribute
    {
    }
}
