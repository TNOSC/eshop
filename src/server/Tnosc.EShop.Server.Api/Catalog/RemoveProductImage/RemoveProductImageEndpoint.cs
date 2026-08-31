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
using Tnosc.EShop.Server.Application.Catalog.Commands.RemoveProductImage;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.RemoveProductImage;

/// <summary>
/// <c>DELETE /api/catalog/products/{id}/image</c> — removes a product's image, if it has one.
/// </summary>
internal sealed class RemoveProductImageEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapDelete(pattern: CatalogRoutes.ProductImage, handler: HandleAsync)
           .WithName(endpointName: "RemoveProductImage")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Remove a product's image")
           .WithDescription(description: "Removes a product's image. A no-op success when the product has none.")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .HasPermission(permission: Permissions.Catalog.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        ICommandHandler<RemoveProductImageCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            command: new RemoveProductImageCommand(ProductId: id),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
