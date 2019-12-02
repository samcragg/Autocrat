// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Abstractions
{
    using System;

    /// <summary>
    /// Marks a class as implementing an interface via static methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RewriteInterfaceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RewriteInterfaceAttribute"/> class.
        /// </summary>
        /// <param name="interfaceType">The type of interface to replace.</param>
        public RewriteInterfaceAttribute(Type interfaceType)
        {
            this.InterfaceType = interfaceType;
        }

        /// <summary>
        /// Gets the type of the interface that will be replaced.
        /// </summary>
        public Type InterfaceType { get; }
    }
}
