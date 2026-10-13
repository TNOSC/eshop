// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Attributes;

/// <summary>
/// Caches the result of a message handler or component for a duration.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CacheableAttribute"/> class.
/// </remarks>
/// <param name="durationSeconds">Cache duration in seconds. Default is 60.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheableAttribute(int durationSeconds = 60) : Attribute
{
    /// <summary>
    /// Gets the cache duration in seconds.
    /// </summary>
    public int DurationSeconds { get; } = durationSeconds;
}
