// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Transform.Managed
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Mono.Cecil;

    /// <summary>
    /// Allows the comparing of <see cref="TypeReference"/> instances.
    /// </summary>
    internal class TypeReferenceEqualityComparer : EqualityComparer<TypeReference>
    {
        /// <inheritdoc />
        public override bool Equals([AllowNull] TypeReference x, [AllowNull] TypeReference y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }
            else if ((x is null) || (y is null))
            {
                return false;
            }
            else
            {
                return x.FullName.Equals(y.FullName, StringComparison.Ordinal);
            }
        }

        /// <inheritdoc />
        public override int GetHashCode([DisallowNull] TypeReference obj)
        {
            if (obj is null)
            {
                return 0;
            }
            else
            {
                return obj.FullName.GetHashCode(StringComparison.Ordinal);
            }
        }
    }
}
