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
using Tnosc.EShop.Server.Application.Catalog.Queries.SearchProducts;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Api.Catalog.SearchProducts;

/// <summary>
/// <c>GET /api/catalog/products</c> — searches the catalogue, one page at a time.
/// </summary>
internal sealed class SearchProductsEndpoint : IApiEndpoint
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    /// <inheritdoc />
    public void MapEndpoint(WebApplication app) =>
        app.MapGet(pattern: CatalogRoutes.Products, handler: HandleAsync)
           .WithName(endpointName: "SearchProducts")
           .WithTags(CatalogRoutes.Tag)
           .WithSummary(summary: "Search products")
           .WithDescription(
               description: "Searches the catalogue by free-text term and/or category, returning one " +
                             "page of product summaries at a time.")
           .Produces<PagedResult<ProductSummaryDto>>(statusCode: StatusCodes.Status200OK);

    private static async Task<IResult> HandleAsync(
        IQueryHandler<SearchProductsQuery, PagedResult<ProductSummaryDto>> handler,
        CancellationToken cancellationToken,
        string? search = null,
        Guid? categoryId = null,
        int page = DefaultPage,
        int pageSize = DefaultPageSize)
    {
        Result<PagedResult<ProductSummaryDto>> result = await handler.HandleAsync(
            query: new SearchProductsQuery(
                SearchTerm: search,
                CategoryId: categoryId,
                Page: page,
                PageSize: pageSize),
            cancellationToken: cancellationToken);

        return result.ToHttp(onSuccess: static products => Results.Ok(value: products));
    }
}
