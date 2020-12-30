// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler.CodeGeneration
{
    using Mono.Cecil;

    /// <summary>
    /// Contains constants used for generating metadata.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Represents the special name for a constructor method.
        /// </summary>
        public const string Constructor = ".ctor";

        /// <summary>
        /// Represents the attribute for a private instance method.
        /// </summary>
        public const MethodAttributes PrivateMethod =
            MethodAttributes.HideBySig | MethodAttributes.Private;

        /// <summary>
        /// Represents the attributes for a private field marked as readonly.
        /// </summary>
        public const FieldAttributes PrivateReadonly =
            FieldAttributes.InitOnly | FieldAttributes.Private;

        /// <summary>
        /// Represents the attributes for a public constructor.
        /// </summary>
        public const MethodAttributes PublicConstructor =
            MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;

        /// <summary>
        /// Represents the attribute for a public instance method.
        /// </summary>
        public const MethodAttributes PublicMethod =
            MethodAttributes.HideBySig | MethodAttributes.Public;

        /// <summary>
        /// Represents the attribute for a public static method.
        /// </summary>
        public const MethodAttributes PublicStaticMethod =
            MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static;
    }
}
