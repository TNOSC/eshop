// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Contracts.Common;

/// <summary>
/// A single page of <typeparamref name="TItem"/> as returned by the server's paged endpoints.
/// </summary>
/// <typeparam name="TItem">The type of item contained in the page.</typeparam>
public sealed record PagedResult<TItem>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages);
