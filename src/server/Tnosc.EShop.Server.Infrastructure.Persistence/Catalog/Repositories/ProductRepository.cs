// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Infrastructure.Persistence;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Repositories;

/// <summary>
/// The write-side <see cref="Product"/> repository.
/// </summary>
/// <param name="context">The write context this repository reads and writes through.</param>
internal sealed class ProductRepository(EShopWriteDbContext context)
    : RepositoryBase<Product, ProductId>(context), IProductRepository
{
    /// <inheritdoc />
    public async ValueTask<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        await context.Set<Product>()
            .FirstOrDefaultAsync(
                predicate: product => product.Sku.Value == sku.Value,
                cancellationToken: cancellationToken);
}
