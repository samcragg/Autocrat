// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System;

    /// <summary>
    /// Marks a class as providing configuration data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ConfigurationAttribute : Attribute
    {
    }
}
