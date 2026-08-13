// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Ordering.Queries.GetMyOrders;

/// <summary>
/// Reads a page of the caller's own order history, newest first.
/// </summary>
/// <param name="CustomerId">The identifier of the customer whose orders to list, taken from the caller's token.</param>
/// <param name="Page">The one-based page to read.</param>
/// <param name="PageSize">How many orders a page holds. Clamped by the handler.</param>
public sealed record GetMyOrdersQuery(
    Guid CustomerId,
    int Page,
    int PageSize) : IQuery<PagedResult<OrderSummaryDto>>;
