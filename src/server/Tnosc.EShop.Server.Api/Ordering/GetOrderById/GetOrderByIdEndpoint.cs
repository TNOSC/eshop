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
using Tnosc.EShop.Server.Application.Ordering.Queries.GetOrderById;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Ordering.GetOrderById;

/// <summary>
/// <c>GET /api/orders/{id}</c> — reads one of the caller's own orders, with its lines.
/// </summary>
/// <remarks>
/// The caller's identity goes into the query alongside the route identifier, so an order belonging to
/// another customer is not selected and the response is a 404 — identical to asking for an order that
/// does not exist. There is no ownership check anywhere, because there is nothing to check.
/// </remarks>
internal sealed class GetOrderByIdEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: OrderingRoutes.OrderById, handler: HandleAsync)
           .WithName(endpointName: "GetOrderById")
           .WithTags(OrderingRoutes.Tag)
           .WithSummary(summary: "Read one of the caller's orders")
           .WithDescription(
               description: "Returns one of the caller's own orders with its lines. An order belonging " +
                             "to another customer returns 404, exactly as an order that does not exist " +
                             "does, so this endpoint cannot be used to discover order identifiers.")
           .Produces<OrderDto>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        Guid id,
        IUserContext userContext,
        IQueryHandler<GetOrderByIdQuery, OrderDto> handler,
        CancellationToken cancellationToken)
    {
        Result<OrderDto> result = await handler.HandleAsync(
            query: new GetOrderByIdQuery(OrderId: id, CustomerId: Guid.Parse(input: userContext.UserId!)),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static order => Results.Ok(value: order));
    }
}
