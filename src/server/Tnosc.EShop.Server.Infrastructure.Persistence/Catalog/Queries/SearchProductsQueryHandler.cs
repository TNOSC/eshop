// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tnosc.EShop.Server.Application.Catalog.Queries.SearchProducts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Queries;

/// <summary>
/// Searches the catalogue with raw SQL: a three-table join, two optional filters and a page window,
/// projected onto a handful of columns.
/// </summary>
/// <remarks>
/// Raw SQL rather than LINQ because this is exactly the shape the design doc reserves it for — joining
/// several tables and projecting a subset of their columns. Every value reaches the database as an
/// <see cref="NpgsqlParameter"/>; nothing is interpolated into the SQL text. <c>COUNT(*) OVER ()</c>
/// carries the unpaged total on every row, so one round trip answers both the page and its count.
/// </remarks>
/// <param name="context">The read context.</param>
internal sealed class SearchProductsQueryHandler(EShopReadDbContext context)
    : IQueryHandler<SearchProductsQuery, PagedResult<ProductSummaryDto>>
{
    /// <summary>
    /// The largest page this query will serve, however large a page the caller asks for.
    /// </summary>
    public const int MaxPageSize = 100;

    private const string SearchSql = """
        SELECT p.id             AS "Id",
               p.sku            AS "Sku",
               p.name           AS "Name",
               p.price_amount   AS "PriceAmount",
               p.price_currency AS "PriceCurrency",
               p.stock_quantity AS "StockQuantity",
               b.name           AS "BrandName",
               c.name           AS "CategoryName",
               p.image_url      AS "ImageUrl",
               COUNT(*) OVER () AS "TotalCount"
        FROM catalog.products p
        INNER JOIN catalog.brands b ON b.id = p.brand_id
        INNER JOIN catalog.categories c ON c.id = p.category_id
        WHERE (@search IS NULL OR p.name ILIKE @search OR p.sku ILIKE @search)
          AND (@category_id IS NULL OR p.category_id = @category_id)
        ORDER BY p.name, p.id
        OFFSET @skip LIMIT @take
        """;

    /// <inheritdoc />
    public async ValueTask<Result<PagedResult<ProductSummaryDto>>> HandleAsync(
        SearchProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = Math.Max(val1: query.Page, val2: 1);
        int pageSize = Math.Clamp(value: query.PageSize, min: 1, max: MaxPageSize);

        List<ProductSearchRow> rows = await context.Database
            .SqlQueryRaw<ProductSearchRow>(
                sql: SearchSql,
                parameters:
                [
                    Parameter(name: "search", type: NpgsqlDbType.Text, value: ToLikePattern(term: query.SearchTerm)),
                    Parameter(name: "category_id", type: NpgsqlDbType.Uuid, value: query.CategoryId),
                    Parameter(name: "skip", type: NpgsqlDbType.Integer, value: (page - 1) * pageSize),
                    Parameter(name: "take", type: NpgsqlDbType.Integer, value: pageSize),
                ])
            .ToListAsync(cancellationToken: cancellationToken);

        IReadOnlyCollection<ProductSummaryDto> items = [.. rows.Select(selector: static row => new ProductSummaryDto(
            Id: row.Id,
            Sku: row.Sku,
            Name: row.Name,
            PriceAmount: row.PriceAmount,
            PriceCurrency: row.PriceCurrency,
            StockQuantity: row.StockQuantity,
            BrandName: row.BrandName,
            CategoryName: row.CategoryName,
            ImageUrl: row.ImageUrl))];

        // Every row carries the same window total; an empty page yields the natural zero.
        long totalCount = rows.Select(selector: static row => row.TotalCount).FirstOrDefault();

        return new PagedResult<ProductSummaryDto>(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount);
    }

    private static string? ToLikePattern(string? term) =>
        term is null ? null : $"%{term}%";

    private static NpgsqlParameter Parameter(string name, NpgsqlDbType type, object? value) =>
        new(parameterName: name, parameterType: type) { Value = value ?? DBNull.Value };
}
