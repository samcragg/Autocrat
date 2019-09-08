// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System;

    /// <summary>
    /// Marks a class as implementing an interface that should be rewritten to
    /// call native methods instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class NativeAdapterAttribute : Attribute
    {
    }
}
