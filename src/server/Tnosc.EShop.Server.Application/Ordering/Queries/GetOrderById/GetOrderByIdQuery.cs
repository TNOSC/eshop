// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderById;

/// <summary>
/// Reads one of the caller's own orders, with its lines.
/// </summary>
/// <remarks>
/// <see cref="CustomerId"/> is filled from the caller's token and is part of the <c>WHERE</c> clause,
/// not a check applied to the row afterwards. An order belonging to someone else is simply not
/// selected, so the endpoint answers <c>404</c> and cannot be used to discover which order
/// identifiers exist.
/// </remarks>
/// <param name="OrderId">The identifier of the order to read.</param>
/// <param name="CustomerId">The identifier of the customer the order must belong to.</param>
public sealed record GetOrderByIdQuery(Guid OrderId, Guid CustomerId) : IQuery<OrderDto>;
