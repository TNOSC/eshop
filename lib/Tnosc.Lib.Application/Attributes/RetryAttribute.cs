// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Attributes;

/// <summary>
/// Specifies retry behavior for a message handler or component.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RetryAttribute : Attribute
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryAttribute"/> class.
    /// </summary>
    /// <param name="maxRetries">Maximum retry attempts. Default is 3.</param>
    public RetryAttribute(int maxRetries = 3) => MaxRetries = maxRetries;
}
