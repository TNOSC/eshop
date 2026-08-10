// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Catalog.Queries.GetProductById;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Queries;

/// <summary>
/// Projects a single product straight from the read context into <see cref="ProductDto"/>.
/// </summary>
/// <param name="context">The read context.</param>
internal sealed class GetProductByIdQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<ProductDto>> HandleAsync(
        GetProductByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ProductDto? product = await context.Set<ProductReadModel>()
            .Where(predicate: readModel => readModel.Id == query.ProductId)
            .Select(selector: readModel => new ProductDto(
                Id: readModel.Id,
                Sku: readModel.Sku,
                Name: readModel.Name,
                Description: readModel.Description,
                PriceAmount: readModel.PriceAmount,
                PriceCurrency: readModel.PriceCurrency,
                StockQuantity: readModel.StockQuantity,
                BrandId: readModel.BrandId,
                CategoryId: readModel.CategoryId,
                IsDiscontinued: readModel.IsDiscontinued))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (product is null)
        {
            return ProductErrors.NotFound(productId: query.ProductId);
        }

        return product;
    }
}
