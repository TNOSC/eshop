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
using Tnosc.EShop.Server.Application.Basket.Commands.RemoveBasketItem;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Contexts;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Basket.RemoveBasketItem;

/// <summary>
/// <c>DELETE /api/basket/items/{itemId}</c> — removes one line from the caller's basket.
/// </summary>
internal sealed class RemoveBasketItemEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapDelete(pattern: BasketRoutes.CurrentBasketItemById, handler: HandleAsync)
           .WithName(endpointName: "RemoveBasketItem")
           .WithTags(BasketRoutes.Tag)
           .WithSummary(summary: "Remove a line from the caller's basket")
           .WithDescription(description: "Removes one line from the caller's own basket.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        IUserContext userContext,
        ICommandHandler<RemoveBasketItemCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new RemoveBasketItemCommand(
                CustomerId: Guid.Parse(input: userContext.UserId!),
                ItemId: itemId),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
