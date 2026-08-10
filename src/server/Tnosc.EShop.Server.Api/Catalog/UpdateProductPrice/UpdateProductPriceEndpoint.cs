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
using Tnosc.EShop.Server.Application.Catalog.Commands.UpdateProductPrice;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.UpdateProductPrice;

/// <summary>
/// <c>PUT /api/catalog/products/{id}/price</c> — reprices a product.
/// </summary>
internal sealed class UpdateProductPriceEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPut(pattern: CatalogRoutes.ProductPrice, handler: HandleAsync)
           .WithName(endpointName: "UpdateProductPrice")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Update a product's price")
           .WithDescription(
               description: "Sets a new price on a product. Discontinued products cannot be repriced.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateProductPriceRequest request,
        ICommandHandler<UpdateProductPriceCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: request.ToCommand(productId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
