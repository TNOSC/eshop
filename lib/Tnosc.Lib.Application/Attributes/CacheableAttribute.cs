// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Attributes;

/// <summary>
/// Caches the result of a message handler or component for a duration.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheableAttribute : Attribute
{
    /// <summary>
    /// Gets the cache duration in seconds.
    /// </summary>
    public int DurationSeconds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheableAttribute"/> class.
    /// </summary>
    /// <param name="durationSeconds">Cache duration in seconds. Default is 60.</param>
    public CacheableAttribute(int durationSeconds = 60) => DurationSeconds = durationSeconds;
}
