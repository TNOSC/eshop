// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderSummary;

/// <summary>
/// Reads the rolled-up back-office summary of any order.
/// </summary>
/// <remarks>
/// No customer identifier, unlike <c>GetOrderByIdQuery</c>: this is an operator's view of any order,
/// gated by the <c>ordering:read</c> permission at the endpoint rather than scoped to the caller.
/// </remarks>
/// <param name="OrderId">The identifier of the order to summarise.</param>
public sealed record GetOrderSummaryQuery(Guid OrderId) : IQuery<OrderSummaryReportDto>;
