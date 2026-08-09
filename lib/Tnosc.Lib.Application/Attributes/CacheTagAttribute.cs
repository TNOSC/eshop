// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Attributes;

/// <summary>
/// Associates a message handler with a cache tag, used to group cached query results
/// so they can be invalidated together when a related command succeeds.
/// </summary>
/// <param name="tag">The cache tag.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CacheTagAttribute(string tag) : Attribute
{
    /// <summary>
    /// Gets the cache tag.
    /// </summary>
    public string Tag { get; } = tag;
}
