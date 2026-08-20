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
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Abstractions.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Basket.GetBasket;

/// <summary>
/// <c>GET /api/basket</c> — reads the caller's own basket.
/// </summary>
internal sealed class GetBasketEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: BasketRoutes.CurrentBasket, handler: HandleAsync)
           .WithName(endpointName: "GetBasket")
           .WithTags(BasketRoutes.Tag)
           .WithSummary(summary: "Get the caller's basket")
           .WithDescription(
               description: "Reads the caller's own basket. A customer with no basket yet gets back an " +
                             "empty one rather than a 404 — the basket is created lazily on the first item added.")
           .Produces<BasketDto>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .RequireAuthorization();

    private static async Task<IResult> HandleAsync(
        IUserContext userContext,
        IQueryHandler<GetBasketQuery, BasketDto> handler,
        CancellationToken cancellationToken)
    {
        Result<BasketDto> result = await handler.HandleAsync(
            query: new GetBasketQuery(CustomerId: Guid.Parse(input: userContext.UserId!)),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static basket => Results.Ok(value: basket));
    }
}
