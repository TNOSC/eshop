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
using Tnosc.EShop.Server.Application.Catalog.Commands.CreateProduct;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.CreateProduct;

/// <summary>
/// <c>POST /api/catalog/products</c> — adds a product to the catalogue.
/// </summary>
/// <remarks>
/// The <c>Idempotency-Key</c> header is required: the handler is <c>[Idempotent]</c>, so
/// <c>RequestContextMiddleware</c> lifts the header into the ambient key context and the decorator
/// pipeline rejects a request that arrives without one. The key is not a parameter here — no
/// endpoint reads headers directly, and the value never reaches the command.
/// </remarks>
internal sealed class CreateProductEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: CatalogRoutes.Products, handler: HandleAsync)
           .WithName(endpointName: "CreateProduct")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Create a product")
           .WithDescription(
               description: "Adds a new product to the catalogue. The SKU must be unique across the " +
                             "whole catalogue; creation fails with a conflict if it is already taken. " +
                             "Requires an Idempotency-Key header: retrying with the same key and the " +
                             "same body replays the original 201 and its product id instead of " +
                             "creating a second product, reusing a key with a different body returns " +
                             "409, and omitting it returns 400.")
           .Produces<Guid>(statusCode: StatusCodes.Status201Created)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        CreateProductRequest request,
        ICommandHandler<CreateProductCommand, ProductId> handler,
        CancellationToken cancellationToken)
    {
        Result<ProductId> result = await handler.HandleAsync(
            command: request.ToCommand(),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static productId =>
            Results.Created(uri: $"{CatalogRoutes.Products}/{productId.Value}", value: productId.Value));
    }
}
