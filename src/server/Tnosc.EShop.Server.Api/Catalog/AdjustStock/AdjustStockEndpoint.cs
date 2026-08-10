// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tnosc.EShop.Server.Application.Catalog.Commands.AdjustStock;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.AdjustStock;

/// <summary>
/// <c>POST /api/catalog/products/{id}/stock</c> — adds or removes units from a product's stock level.
/// </summary>
internal sealed class AdjustStockEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: CatalogRoutes.ProductStock, handler: HandleAsync)
           .WithName(endpointName: "AdjustStock")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Adjust a product's stock level")
           .WithDescription(
               description: "Adds or removes units from a product's stock level by a signed delta. " +
                             "The delta must be non-zero and the resulting stock level cannot go below zero.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        Guid id,
        AdjustStockRequest request,
        ICommandHandler<AdjustStockCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: request.ToCommand(productId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
