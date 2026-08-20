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
using Tnosc.EShop.Server.Application.Ordering.Commands.ShipOrder;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Ordering.ShipOrder;

/// <summary>
/// <c>POST /api/orders/{id}/ship</c> — despatches an order.
/// </summary>
/// <remarks>
/// The one Ordering endpoint that names a permission. Shipping is a warehouse operation over any
/// customer's order, so it cannot be scoped to the caller the way confirming and cancelling are — and
/// where ownership cannot do the work, a permission has to.
/// </remarks>
internal sealed class ShipOrderEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: OrderingRoutes.OrderShip, handler: HandleAsync)
           .WithName(endpointName: "ShipOrder")
           .WithTags(OrderingRoutes.Tag)
           .WithSummary(summary: "Despatch an order")
           .WithDescription(
               description: "Marks an order as shipped. Only a paid order may be despatched: attempting " +
                             "to ship one that is still pending, confirmed, already shipped, delivered " +
                             "or cancelled returns 409 with the Order.CannotShip code and the status " +
                             "the order was actually in.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .HasPermission(permission: Permissions.Ordering.Ship);

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICommandHandler<ShipOrderCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new ShipOrderCommand(OrderId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
