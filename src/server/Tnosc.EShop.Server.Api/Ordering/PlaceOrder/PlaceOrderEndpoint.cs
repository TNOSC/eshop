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
using Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Ordering.PlaceOrder;

/// <summary>
/// <c>POST /api/orders</c> — turns the caller's basket into an order.
/// </summary>
/// <remarks>
/// No request body: everything the order needs is resolved server-side from the caller's basket and
/// profile, so there is nothing for a client to supply and nothing for it to tamper with.
/// The <c>Idempotency-Key</c> header is required — the handler is <c>[Idempotent]</c>, so
/// <c>RequestContextMiddleware</c> lifts the header into the ambient key context and the decorator
/// pipeline rejects a request that arrives without one.
/// </remarks>
internal sealed class PlaceOrderEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: OrderingRoutes.Orders, handler: HandleAsync)
           .WithName(endpointName: "PlaceOrder")
           .WithTags(OrderingRoutes.Tag)
           .WithSummary(summary: "Place an order from the caller's basket")
           .WithDescription(
               description: "Turns the caller's current basket into an order: the lines and the prices " +
                             "the customer saw are snapshotted onto the order, the delivery address is " +
                             "copied from their default address, and any discount their history earns " +
                             "is applied. Fails with 409 when the basket is empty, when the customer " +
                             "has no default address, or when the catalogue cannot supply a line. " +
                             "Requires an Idempotency-Key header: retrying with the same key replays " +
                             "the original 201 and its order id instead of placing a second order, " +
                             "reusing a key with a different body returns 409, and omitting it " +
                             "returns 400. The basket is cleared and catalogue stock decremented " +
                             "asynchronously, once the outbox delivers the order-placed event.")
           .Produces<Guid>(statusCode: StatusCodes.Status201Created)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        IUserContext userContext,
        ICommandHandler<PlaceOrderCommand, OrderId> handler,
        CancellationToken cancellationToken)
    {
        Result<OrderId> result = await handler.HandleAsync(
            command: new PlaceOrderCommand(CustomerId: Guid.Parse(input: userContext.UserId!)),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static orderId =>
            Results.Created(uri: $"{OrderingRoutes.Orders}/{orderId.Value}", value: orderId.Value));
    }
}
