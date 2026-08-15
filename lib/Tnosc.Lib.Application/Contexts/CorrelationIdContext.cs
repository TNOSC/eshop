// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;

namespace Tnosc.Lib.Application.Contexts;

/// <summary>
/// Ambient, async-flowing correlation id used as the default for exceptions that
/// don't explicitly receive one, so callers don't need to thread a correlation id
/// through every layer by hand.
/// </summary>
public static class CorrelationIdContext
{
    private static readonly AsyncLocal<string?> _current = new();

    /// <summary>
    /// Gets or sets the correlation id for the current logical call context (for example, the current HTTP request).
    /// </summary>
    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
