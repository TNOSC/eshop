// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tnosc.EShop.Server.Application.Catalog.Queries.GetProductById;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Queries;

/// <summary>
/// Projects a single product, joined with its brand, from the read context into <see cref="ProductDto"/>.
/// </summary>
/// <param name="context">The read context.</param>
internal sealed class GetProductByIdQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    private const string ProductByIdSql = """
        SELECT p.id             AS "Id",
               p.sku            AS "Sku",
               p.name           AS "Name",
               p.description    AS "Description",
               p.price_amount   AS "PriceAmount",
               p.price_currency AS "PriceCurrency",
               p.stock_quantity AS "StockQuantity",
               p.brand_id       AS "BrandId",
               b.name           AS "BrandName",
               p.category_id    AS "CategoryId",
               p.is_discontinued AS "IsDiscontinued"
        FROM catalog.products p
        INNER JOIN catalog.brands b ON b.id = p.brand_id
        WHERE p.id = @id
        """;

    /// <inheritdoc />
    public async ValueTask<Result<ProductDto>> HandleAsync(
        GetProductByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        List<ProductByIdRow> rows = await context.Database
            .SqlQueryRaw<ProductByIdRow>(
                sql: ProductByIdSql,
                parameters: [new NpgsqlParameter(parameterName: "id", parameterType: NpgsqlDbType.Uuid) { Value = query.ProductId }])
            .ToListAsync(cancellationToken: cancellationToken);

        ProductByIdRow? row = rows.SingleOrDefault();

        if (row is null)
        {
            return ProductErrors.NotFound(productId: query.ProductId);
        }

        return new ProductDto(
            Id: row.Id,
            Sku: row.Sku,
            Name: row.Name,
            Description: row.Description,
            PriceAmount: row.PriceAmount,
            PriceCurrency: row.PriceCurrency,
            StockQuantity: row.StockQuantity,
            BrandId: row.BrandId,
            BrandName: row.BrandName,
            CategoryId: row.CategoryId,
            IsDiscontinued: row.IsDiscontinued);
    }
}
