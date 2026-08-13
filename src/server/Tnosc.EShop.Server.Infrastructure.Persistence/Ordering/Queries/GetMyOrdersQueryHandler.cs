// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetMyOrders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.ReadModels;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Queries;

/// <summary>
/// Lists a customer's own orders, newest first, without their lines.
/// </summary>
/// <remarks>
/// The line count is projected with a <c>COUNT</c> over the navigation rather than by loading the
/// lines, so a customer with a long history does not drag every line of every order across the wire
/// to render a list that shows none of them.
/// </remarks>
/// <param name="context">The read context.</param>
internal sealed class GetMyOrdersQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetMyOrdersQuery, PagedResult<OrderSummaryDto>>
{
    /// <summary>
    /// The largest page this query will serve, however large a page the caller asks for.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <inheritdoc />
    public async ValueTask<Result<PagedResult<OrderSummaryDto>>> HandleAsync(
        GetMyOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = Math.Max(val1: query.Page, val2: 1);
        int pageSize = Math.Clamp(value: query.PageSize, min: 1, max: MaxPageSize);

        IQueryable<OrderReadModel> filtered = context.Set<OrderReadModel>()
            .Where(predicate: order => order.CustomerId == query.CustomerId);

        long totalCount = await filtered.LongCountAsync(cancellationToken: cancellationToken);

        List<OrderSummaryDto> items = await filtered
            .OrderByDescending(keySelector: static order => order.PlacedOnUtc)
            .ThenByDescending(keySelector: static order => order.Id)
            .Skip(count: (page - 1) * pageSize)
            .Take(count: pageSize)
            .Select(selector: static order => new OrderSummaryDto(
                Id: order.Id,
                OrderNumber: order.OrderNumber,
                Status: order.Status,
                TotalAmount: order.TotalAmount,
                TotalCurrency: order.TotalCurrency,
                PlacedOnUtc: order.PlacedOnUtc,
                LineCount: order.Lines.Count))
            .ToListAsync(cancellationToken: cancellationToken);

        return new PagedResult<OrderSummaryDto>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount);
    }
}
