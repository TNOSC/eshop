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
using Tnosc.EShop.Server.Application.Ordering.Commands.ConfirmOrder;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Ordering.ConfirmOrder;

/// <summary>
/// <c>POST /api/orders/{id}/confirm</c> — confirms one of the caller's own pending orders.
/// </summary>
internal sealed class ConfirmOrderEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: OrderingRoutes.OrderConfirm, handler: HandleAsync)
           .WithName(endpointName: "ConfirmOrder")
           .WithTags(OrderingRoutes.Tag)
           .WithSummary(summary: "Confirm one of the caller's orders")
           .WithDescription(
               description: "Moves one of the caller's own pending orders to confirmed, making it " +
                             "payable. An order that is not pending returns 409 with the transition " +
                             "the aggregate refused; an order belonging to another customer returns 404.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        Guid id,
        IUserContext userContext,
        ICommandHandler<ConfirmOrderCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new ConfirmOrderCommand(OrderId: id, CustomerId: Guid.Parse(input: userContext.UserId!)),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
