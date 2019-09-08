// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "The assembly will not be localized")]

[assembly: SuppressMessage(
    "Major Code Smell",
    "S4200:Native methods should be wrapped",
    Justification = "These methods are implemented by the linked native code")]

[assembly: SuppressMessage(
    "Major Code Smell",
    "S4214:\"P/Invoke\" methods should not be visible",
    Justification = "These methods are implemented by the linked native code")]

[assembly: SuppressMessage(
    "Design",
    "CA1060:Move pinvokes to native methods class",
    Justification = "These methods are implemented by the linked native code")]

[assembly: SuppressMessage(
    "Interoperability",
    "CA1401:P/Invokes should not be visible",
    Justification = "These methods are implemented by the linked native code")]
