// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Attributes;

/// <summary>
/// Marks a query property as part of the cache key used to store and retrieve its cached result.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CacheKeyAttribute : Attribute { }
