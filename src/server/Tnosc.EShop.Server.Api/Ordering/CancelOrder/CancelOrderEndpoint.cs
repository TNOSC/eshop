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
using Tnosc.EShop.Server.Application.Ordering.Commands.CancelOrder;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Ordering.CancelOrder;

/// <summary>
/// <c>POST /api/orders/{id}/cancel</c> — cancels one of the caller's own orders before it ships.
/// </summary>
internal sealed class CancelOrderEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: OrderingRoutes.OrderCancel, handler: HandleAsync)
           .WithName(endpointName: "CancelOrder")
           .WithTags(OrderingRoutes.Tag)
           .WithSummary(summary: "Cancel one of the caller's orders")
           .WithDescription(
               description: "Cancels one of the caller's own orders. Reachable up to despatch: an order " +
                             "already shipped, delivered or cancelled returns 409, because reversing " +
                             "one of those is a return rather than a cancellation. An order belonging " +
                             "to another customer returns 404.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        Guid id,
        IUserContext userContext,
        ICommandHandler<CancelOrderCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new CancelOrderCommand(OrderId: id, CustomerId: Guid.Parse(input: userContext.UserId!)),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
