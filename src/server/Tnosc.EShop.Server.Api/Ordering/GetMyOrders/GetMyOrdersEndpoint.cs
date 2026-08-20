// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tnosc.EShop.Server.Application.Ordering.Queries.GetMyOrders;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Ordering.GetMyOrders;

/// <summary>
/// <c>GET /api/orders</c> — lists the caller's own order history, newest first.
/// </summary>
internal sealed class GetMyOrdersEndpoint : IApiEndpoint
{
    private const int DefaultPage = 1;

    private const int DefaultPageSize = 20;

    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: OrderingRoutes.Orders, handler: HandleAsync)
           .WithName(endpointName: "GetMyOrders")
           .WithTags(OrderingRoutes.Tag)
           .WithSummary(summary: "List the caller's orders")
           .WithDescription(
               description: "Returns a page of the caller's own orders, newest first, without their " +
                             "lines. The page size is clamped server-side, so a caller cannot ask for " +
                             "an unbounded page.")
           .Produces<PagedResult<OrderSummaryDto>>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        IUserContext userContext,
        IQueryHandler<GetMyOrdersQuery, PagedResult<OrderSummaryDto>> handler,
        CancellationToken cancellationToken,
        int page = DefaultPage,
        int pageSize = DefaultPageSize)
    {
        Result<PagedResult<OrderSummaryDto>> result = await handler.HandleAsync(
            query: new GetMyOrdersQuery(
                CustomerId: Guid.Parse(input: userContext.UserId!),
                Page: page,
                PageSize: pageSize),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static orders => Results.Ok(value: orders));
    }
}
