// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Reflection;

namespace Tnosc.EShop.Mcp.Infrastructure.External;

/// <summary>
/// A handle to this assembly, used by whatever needs to scan it.
/// </summary>
public static class AssemblyReference
{
    /// <summary>The assembly this type is declared in.</summary>
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
