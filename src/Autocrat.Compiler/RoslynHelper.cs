// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Autocrat.Compiler
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// Provides utility methods for working with the Roslyn API.
    /// </summary>
    internal static class RoslynHelper
    {
        /// <summary>
        /// Generates an attribute indicating that the method can be called by
        /// native code.
        /// </summary>
        /// <param name="method">The native name of the method.</param>
        /// <returns>An attribute syntax.</returns>
        public static AttributeSyntax CreateNativeCallableAttribute(string method)
        {
            AttributeArgumentSyntax callingConvention = AttributeArgument(
                NameEquals(IdentifierName("CallingConvention")),
                null,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(nameof(CallingConvention)),
                    IdentifierName(nameof(CallingConvention.Cdecl))));

            AttributeArgumentSyntax entryPoint = AttributeArgument(
                NameEquals(IdentifierName("EntryPoint")),
                null,
                LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(method)));

            var arguments = new AttributeArgumentSyntax[]
            {
                entryPoint,
                callingConvention,
            };

            return Attribute(IdentifierName("NativeCallable"))
                .WithArgumentList(AttributeArgumentList(SeparatedList(arguments)));
        }

        /// <summary>
        /// Finds an attribute of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the attribute to find.</typeparam>
        /// <param name="type">The type to search for the attribute on.</param>
        /// <returns>The attribute data if found; otherwise, <c>null</c>.</returns>
        public static AttributeData? FindAttribute<T>(ITypeSymbol type)
        {
            foreach (AttributeData attribute in type.GetAttributes())
            {
                if (IsOfType<T>(attribute.AttributeClass))
                {
                    return attribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Converts the symbol to a string representation.
        /// </summary>
        /// <param name="symbol">The type symbol.</param>
        /// <returns>A formatted string representation of the symbol.</returns>
        public static string GetString(ITypeSymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        /// <summary>
        /// Determines whether the specified symbol equals the specified type.
        /// </summary>
        /// <typeparam name="T">The type to compare to.</typeparam>
        /// <param name="symbol">The type symbol.</param>
        /// <returns>
        /// <c>true</c> if the type and symbol have the same name and namespace;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsOfType<T>(INamedTypeSymbol symbol)
        {
            return string.Equals(symbol.Name, typeof(T).Name, StringComparison.Ordinal) &&
                   string.Equals(symbol.ContainingNamespace.ToDisplayString(), typeof(T).Namespace, StringComparison.Ordinal);
        }
    }
}
