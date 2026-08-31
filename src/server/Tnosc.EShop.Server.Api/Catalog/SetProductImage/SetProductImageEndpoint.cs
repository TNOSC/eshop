// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tnosc.EShop.Server.Application.Catalog.Commands.SetProductImage;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.SetProductImage;

/// <summary>
/// <c>POST /api/catalog/products/{id}/image</c> — uploads a product's image, replacing any previous one.
/// </summary>
internal sealed class SetProductImageEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapPost(pattern: CatalogRoutes.ProductImage, handler: HandleAsync)
           .DisableAntiforgery()
           .WithName(endpointName: "SetProductImage")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Upload a product's image")
           .WithDescription(
               description: "Uploads a new image for a product (image/jpeg, image/png or image/webp, up to 5 MB), replacing any previous one.")
           .Accepts<IFormFile>(contentType: "multipart/form-data")
           .Produces(statusCode: StatusCodes.Status204NoContent)
           .ProducesValidationProblem(statusCode: StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(statusCode: StatusCodes.Status404NotFound)
           .HasPermission(permission: Permissions.Catalog.Write);

    private static async Task<IResult> HandleAsync(
        Guid id,
        IFormFile file,
        ICommandHandler<SetProductImageCommand> handler,
        CancellationToken cancellationToken)
    {
        await using Stream stream = file.OpenReadStream();
        using MemoryStream buffer = new();
        await stream.CopyToAsync(destination: buffer, cancellationToken: cancellationToken);

        Result result = await handler.HandleAsync(
            command: new SetProductImageCommand(
                ProductId: id,
                FileName: file.FileName,
                ContentType: file.ContentType,
                Content: buffer.ToArray()),
            cancellationToken: cancellationToken);

        return result.ToHttp();
    }
}
