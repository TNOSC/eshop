// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Tnosc.EShop.Server.Application.Catalog.Queries.GetCategories;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.GetCategories;

/// <summary>
/// <c>GET /api/catalog/categories</c> — reads every catalogue category. Served from cache between
/// catalogue writes.
/// </summary>
internal sealed class GetCategoriesEndpoint : IApiEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: CatalogRoutes.Categories, handler: HandleAsync)
           .WithName(endpointName: "GetCategories")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "List categories")
           .WithDescription(description: "Reads every category in the catalogue.")
           .Produces<CategoryDto[]>(statusCode: StatusCodes.Status200OK);

    private static async Task<IResult> HandleAsync(
        IQueryHandler<GetCategoriesQuery, CategoryDto[]> handler,
        CancellationToken cancellationToken)
    {
        Result<CategoryDto[]> result = await handler.HandleAsync(
            query: new GetCategoriesQuery(),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static categories => Results.Ok(value: categories));
    }
}
