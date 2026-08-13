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
using Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Basket.AddItemToBasket;

/// <summary>
/// <c>POST /api/basket/items</c> — adds a product to the caller's basket.
/// </summary>
internal sealed class AddItemToBasketEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: BasketRoutes.CurrentBasketItems, handler: HandleAsync)
           .WithName(endpointName: "AddItemToBasket")
           .WithTags(BasketRoutes.Tag)
           .WithSummary(summary: "Add a product to the caller's basket")
           .WithDescription(
               description: "Adds a product to the caller's own basket, snapshotting its current SKU, " +
                             "name and price. Adding a product already present increments that line's " +
                             "quantity instead of creating a second line. The basket is created lazily " +
                             "on the first call.")
           .Produces<BasketDto>(statusCode: StatusCodes.Status200OK)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        AddItemToBasketRequest request,
        IUserContext userContext,
        ICommandHandler<AddItemToBasketCommand, BasketDto> handler,
        CancellationToken cancellationToken)
    {
        Result<BasketDto> result = await handler.HandleAsync(
            command: request.ToCommand(customerId: Guid.Parse(input: userContext.UserId!)),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static basket => Results.Ok(value: basket));
    }
}
