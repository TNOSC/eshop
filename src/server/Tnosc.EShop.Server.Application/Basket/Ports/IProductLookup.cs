// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Server.Application.Basket.Ports;

/// <summary>
/// Reads the catalogue data a basket line needs to snapshot when a product is added.
/// </summary>
/// <remarks>
/// Owned by Basket rather than Catalog: Basket must not reference Catalog's types, but it still needs
/// to know whether a product exists and what to snapshot about it. Implemented in
/// <c>Server.Infrastructure.Persistence</c> by querying <c>EShopReadDbContext</c> — a genuine Postgres
/// read, which is why that implementation cannot move to <c>Server.Infrastructure.External</c> despite
/// every other piece of Basket infrastructure living there.
/// </remarks>
public interface IProductLookup
{
    /// <summary>
    /// Reads the snapshot data for a product.
    /// </summary>
    /// <param name="productId">The identifier of the product to look up.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The product's snapshot data, or <see langword="null"/> when no such product exists.</returns>
    ValueTask<ProductSnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default);
}
