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
using Tnosc.EShop.Server.Application.Basket.Commands.ClearBasket;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Basket.ClearBasket;

/// <summary>
/// <c>DELETE /api/basket</c> — empties the caller's basket.
/// </summary>
internal sealed class ClearBasketEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapDelete(pattern: BasketRoutes.CurrentBasket, handler: HandleAsync)
           .WithName(endpointName: "ClearBasket")
           .WithTags(BasketRoutes.Tag)
           .WithSummary(summary: "Clear the caller's basket")
           .WithDescription(description: "Empties the caller's own basket. A customer with no basket yet is a no-op.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        IUserContext userContext,
        ICommandHandler<ClearBasketCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new ClearBasketCommand(CustomerId: Guid.Parse(input: userContext.UserId!)),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
