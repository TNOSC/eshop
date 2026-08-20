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
using Tnosc.EShop.Server.Application.Basket.Commands.ChangeBasketItemQuantity;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Basket.ChangeBasketItemQuantity;

/// <summary>
/// <c>PUT /api/basket/items/{itemId}</c> — replaces the quantity of one line in the caller's basket.
/// </summary>
internal sealed class ChangeBasketItemQuantityEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPut(pattern: BasketRoutes.CurrentBasketItemById, handler: HandleAsync)
           .WithName(endpointName: "ChangeBasketItemQuantity")
           .WithTags(BasketRoutes.Tag)
           .WithSummary(summary: "Change the quantity of a line in the caller's basket")
           .WithDescription(description: "Replaces the quantity of one line in the caller's own basket.")
           .Produces<BasketDto>(statusCode: StatusCodes.Status200OK)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ChangeBasketItemQuantityRequest request,
        IUserContext userContext,
        ICommandHandler<ChangeBasketItemQuantityCommand, BasketDto> handler,
        CancellationToken cancellationToken)
    {
        Result<BasketDto> result = await handler.HandleAsync(
            command: request.ToCommand(customerId: Guid.Parse(input: userContext.UserId!), itemId: itemId),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static basket => Results.Ok(value: basket));
    }
}
