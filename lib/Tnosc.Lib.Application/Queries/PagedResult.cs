// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.Lib.Application.Queries;

/// <summary>
/// One page of query results together with the paging metadata a caller needs to request the next one.
/// </summary>
/// <typeparam name="TItem">The type of the items on the page.</typeparam>
/// <param name="Items">The items on the requested page.</param>
/// <param name="Page">The one-based page number that was requested.</param>
/// <param name="PageSize">The maximum number of items a page holds.</param>
/// <param name="TotalCount">The total number of items matching the query across every page.</param>
public sealed record PagedResult<TItem>(
    IReadOnlyCollection<TItem> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    /// <summary>
    /// Gets the total number of pages the result spans.
    /// </summary>
    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(a: (double)TotalCount / PageSize);
}
