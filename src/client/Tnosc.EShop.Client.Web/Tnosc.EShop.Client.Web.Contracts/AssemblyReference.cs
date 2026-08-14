// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Reflection;

namespace Tnosc.EShop.Client.Web.Contracts;

/// <summary>
/// Provides a stable handle to this assembly, for reflection-based scanning by consumers.
/// </summary>
public static class AssemblyReference
{
    /// <summary>Gets this assembly.</summary>
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
