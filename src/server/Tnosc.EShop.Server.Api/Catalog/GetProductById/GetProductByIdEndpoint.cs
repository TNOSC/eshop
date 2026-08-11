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
using Tnosc.EShop.Server.Application.Catalog.Queries.GetProductById;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.GetProductById;

/// <summary>
/// <c>GET /api/catalog/products/{id}</c> — reads a single product.
/// </summary>
internal sealed class GetProductByIdEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: CatalogRoutes.ProductById, handler: HandleAsync)
           .WithName(endpointName: "GetProductById")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Get a product by identifier")
           .WithDescription(description: "Reads a single product's full details by its identifier.")
           .Produces<ProductDto>(statusCode: StatusCodes.Status200OK)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        Guid id,
        IQueryHandler<GetProductByIdQuery, ProductDto> handler,
        CancellationToken cancellationToken)
    {
        Result<ProductDto> result = await handler.HandleAsync(
            query: new GetProductByIdQuery(ProductId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static product => Results.Ok(value: product));
    }
}
