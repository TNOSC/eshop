// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain.Repositories;

namespace Tnosc.EShop.Server.Domain.Catalog.Products;

/// <summary>
/// Command-side persistence contract for <see cref="Product"/>.
/// </summary>
/// <remarks>
/// Lives in the domain layer because <see cref="ProductFactory"/> — a domain type — needs
/// <see cref="GetBySkuAsync"/> to enforce the catalogue-wide SKU uniqueness invariant.
/// </remarks>
public interface IProductRepository : IRepository<Product, ProductId>
{
    /// <summary>
    /// Retrieves the product carrying the specified SKU.
    /// </summary>
    /// <param name="sku">The stock-keeping unit to look up.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching product, or <see langword="null"/> if no product carries that SKU.</returns>
    ValueTask<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default);
}
